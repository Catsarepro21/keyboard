#![no_main]
#![no_std]

mod board;

use rmk::macros::rmk_keyboard;

#[rmk_keyboard]
mod keyboard {
    #[Override(bind_interrupt)]
    fn interrupts() {
        embassy_rp::bind_interrupts!(struct Irqs {
            USBCTRL_IRQ =>
                embassy_rp::usb::InterruptHandler<embassy_rp::peripherals::USB>;

            DMA_IRQ_0 =>
                embassy_rp::dma::InterruptHandler<embassy_rp::peripherals::DMA_CH0>,
                embassy_rp::dma::InterruptHandler<embassy_rp::peripherals::DMA_CH1>,
                embassy_rp::dma::InterruptHandler<embassy_rp::peripherals::DMA_CH2>,
                embassy_rp::dma::InterruptHandler<embassy_rp::peripherals::DMA_CH3>;

            PIO0_IRQ_0 =>
                embassy_rp::pio::InterruptHandler<embassy_rp::peripherals::PIO0>;
        });

        #[embassy_executor::task]
        async fn cyw43_task(
            runner: cyw43::Runner<
                'static,
                cyw43::SpiBus<
                    embassy_rp::gpio::Output<'static>,
                    cyw43_pio::PioSpi<
                        'static,
                        embassy_rp::peripherals::PIO0,
                        0,
                    >,
                >,
                cyw43::Cyw43439,
            >,
        ) -> ! {
            runner.run().await
        }
    }

    #[register_processor]
    fn board_service() -> crate::board::BoardService {
        let mut pio =
            embassy_rp::pio::Pio::new(p.PIO1, crate::board::BoardIrqs);

        let program =
            embassy_rp::pio_programs::ws2812::PioWs2812Program::new(
                &mut pio.common,
            );

        let leds = embassy_rp::pio_programs::ws2812::PioWs2812::new(
            &mut pio.common,
            pio.sm0,
            p.DMA_CH3,
            Irqs,
            p.PIN_26,
            &program,
        );

        crate::board::BoardService::new(p.PIN_21, p.PIN_22, leds)
    }
}