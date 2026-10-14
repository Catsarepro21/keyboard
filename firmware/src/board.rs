use embassy_rp::{
    gpio::{Flex, Pull},
    peripherals::{PIN_21, PIN_22, PIO1},
    Peri,
};
use embassy_rp::pio_programs::ws2812::{Grb, PioWs2812};
use embassy_time::{block_for, Duration, Timer};
use smart_leds::RGB8;

const PCA: u8 = 0x41;
const OLED: u8 = 0x3c;

// Dim blue lighting to keep USB current low.
const USB_COLOR: RGB8 = RGB8 { r: 0, g: 0, b: 8 };

embassy_rp::bind_interrupts!(pub struct BoardIrqs {
    PIO1_IRQ_0 => embassy_rp::pio::InterruptHandler<PIO1>;
});

pub struct BoardService {
    bus: Bus,
    leds: PioWs2812<'static, PIO1, 0, Grb>,
}

impl BoardService {
    pub fn new(
        scl: Peri<'static, PIN_21>,
        sda: Peri<'static, PIN_22>,
        leds: PioWs2812<'static, PIO1, 0, Grb>,
    ) -> Self {
        let mut bus = Bus {
            scl: Flex::new(scl),
            sda: Flex::new(sda),
        };

        bus.scl.set_low();
        bus.sda.set_low();
        bus.scl.set_as_input();
        bus.sda.set_as_input();
        bus.scl.set_pull(Pull::None);
        bus.sda.set_pull(Pull::None);

        Self { bus, leds }
    }

    async fn screen(&mut self, usb: bool, fault: bool) -> Result<(), ()> {
        self.bus.write(
            OLED,
            &[
                0x00, 0xae, 0xd5, 0x80, 0xa8, 0x1f, 0xd3, 0,
                0x40, 0x8d, 0x14, 0x20, 0, 0xa1, 0xc8, 0xda,
                2, 0x81, 0x30, 0xd9, 0xf1, 0xdb, 0x40, 0xa4,
                0xa6, 0xaf,
            ],
        )?;

        self.bus.write(OLED, &[0, 0x21, 0, 127, 0x22, 0, 3])?;

        let mut frame = [0u8; 512];
        text(&mut frame, 4, 12, b"KEEB");
        text(&mut frame, 7, 34, if usb { b"USB" } else { b"BAT" });

        if fault {
            text(&mut frame, 1, 56, b"FAULT");
        }

        for data in frame.chunks(16) {
            let mut packet = [0u8; 17];
            packet[0] = 0x40;
            packet[1..].copy_from_slice(data);
            self.bus.write(OLED, &packet)?;
            Timer::after_millis(1).await;
        }

        Ok(())
    }
}

impl rmk::core_traits::Runnable for BoardService {
    async fn run(&mut self) -> ! {
        let mut last = None;
        let mut fault_latched = false;
        let mut ready = false;

        loop {
            // Initialize output levels before enabling the output pins.
            if !ready {
                self.bus.recover();

                ready = self.bus.write(PCA, &[1, 0])
                    .and_then(|_| self.bus.write(PCA, &[2, 0]))
                    .and_then(|_| self.bus.write(PCA, &[3, 0xfc]))
                    .is_ok();
            }

            let pins = if ready {
                self.bus.read_register(PCA, 0).ok()
            } else {
                None
            };

            let usb = pins.is_some_and(|v| v & 4 == 0);

            let usb_configured = usb
                && rmk::state::current_connection_status().usb
                    == rmk::types::connection::UsbState::Configured;

            if !usb {
                fault_latched = false;
            }

            if usb && pins.is_some_and(|v| v & 8 == 0) {
                fault_latched = true;
            }

            // P0 = RGB enable, P1 = charger enable.
            let enable_rgb = usb_configured && !fault_latched;

            let outputs = if usb_configured {
                2 | if enable_rgb { 1 } else { 0 }
            } else {
                0
            };

            let enabled =
                ready && self.bus.write(PCA, &[1, outputs]).is_ok();

            if pins.is_none() || !enabled {
                let _ = self.bus.write(PCA, &[1, 0]);
                ready = false;
            }

            if last != Some((usb, fault_latched, enabled, enable_rgb)) {
                self.leds
                    .write_slice(&[RGB8 { r: 0, g: 0, b: 0 }; 79])
                    .await;

                if enable_rgb && enabled {
                    Timer::after_millis(2).await;
                    self.leds.write_slice(&[USB_COLOR; 79]).await;
                }

                let _ = self.screen(usb, fault_latched).await;
                last = Some((usb, fault_latched, enabled, enable_rgb));
            }

            Timer::after_millis(250).await;
        }
    }
}

struct Bus {
    scl: Flex<'static>,
    sda: Flex<'static>,
}

impl Bus {
    fn recover(&mut self) {
        self.data(true);

        for _ in 0..9 {
            self.low_clock();
            let _ = self.clock();
        }

        self.stop();
    }

    fn delay() {
        block_for(Duration::from_micros(4));
    }

    fn clock(&mut self) -> Result<(), ()> {
        self.scl.set_as_input();

        for _ in 0..50 {
            if self.scl.is_high() {
                Self::delay();
                return Ok(());
            }

            Self::delay();
        }

        Err(())
    }

    fn low_clock(&mut self) {
        self.scl.set_as_output();
        Self::delay();
    }

    fn data(&mut self, high: bool) {
        if high {
            self.sda.set_as_input();
        } else {
            self.sda.set_as_output();
        }

        Self::delay();
    }

    fn start(&mut self) -> Result<(), ()> {
        self.data(true);
        self.clock()?;

        if !self.sda.is_high() {
            return Err(());
        }

        self.data(false);
        self.low_clock();
        Ok(())
    }

    fn stop(&mut self) {
        self.data(false);
        let _ = self.clock();
        self.data(true);
    }

    fn byte(&mut self, value: u8) -> Result<(), ()> {
        for bit in (0..8).rev() {
            self.data(value & (1 << bit) != 0);
            self.clock()?;
            self.low_clock();
        }

        self.data(true);
        self.clock()?;
        let ack = !self.sda.is_high();
        self.low_clock();

        if ack { Ok(()) } else { Err(()) }
    }

    fn write(&mut self, address: u8, bytes: &[u8]) -> Result<(), ()> {
        let result = (|| {
            self.start()?;
            self.byte(address << 1)?;

            for &value in bytes {
                self.byte(value)?;
            }

            Ok(())
        })();

        self.stop();
        result
    }

    fn read_register(&mut self, address: u8, register: u8) -> Result<u8, ()> {
        self.write(address, &[register])?;

        let result = (|| {
            self.start()?;
            self.byte((address << 1) | 1)?;
            self.data(true);

            let mut value = 0;

            for _ in 0..8 {
                self.clock()?;
                value = (value << 1) | u8::from(self.sda.is_high());
                self.low_clock();
            }

            self.data(true);
            self.clock()?;
            self.low_clock();
            Ok(value)
        })();

        self.stop();
        result
    }
}

// Draw text for the vertically mounted 128 × 32 OLED.
fn text(frame: &mut [u8; 512], x: usize, y: usize, string: &[u8]) {
    for (index, &character) in string.iter().enumerate() {
        let glyph = match character {
            b'K' => [0x7f, 8, 0x14, 0x22, 0x41],
            b'E' => [0x7f, 0x49, 0x49, 0x49, 0x41],
            b'B' => [0x7f, 0x49, 0x49, 0x49, 0x36],
            b'U' => [0x3f, 0x40, 0x40, 0x40, 0x3f],
            b'S' => [0x46, 0x49, 0x49, 0x49, 0x31],
            b'A' => [0x7e, 0x11, 0x11, 0x11, 0x7e],
            b'T' => [1, 1, 0x7f, 1, 1],
            b'F' => [0x7f, 9, 9, 9, 1],
            b'L' => [0x7f, 0x40, 0x40, 0x40, 0x40],
            _ => [0; 5],
        };

        for (dx, &bits) in glyph.iter().enumerate() {
            for dy in 0..7 {
                let lx = x + index * 6 + dx;
                let ly = y + dy;

                if lx < 32 && ly < 128 && bits & (1 << dy) != 0 {
                    let px = ly;
                    let py = 31 - lx;
                    frame[px + (py / 8) * 128] |= 1 << (py % 8);
                }
            }
        }
    }
}