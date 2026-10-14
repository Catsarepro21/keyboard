# KEEB Pico W firmware

Complete source project for the checked 80-key KEEB PCB, with RMK pinned to
`6ea28f7610d8eba549deb042a430c3ba689897b1` and the matching Embassy revision.
Release compilation passed. The assembled hardware has not been tested.

## Flash the supplied UF2

1. Disconnect the battery for the first USB test.
2. Hold BOOTSEL on the Pico W while connecting the Micro-USB data cable.
3. Release BOOTSEL when the `RPI-RP2` drive appears.
4. Copy `KEEB.uf2` to that drive. It will disconnect and restart automatically.
5. Test every key, encoder rotation and push, then OLED and RGB.
6. Connect the battery after confirming the battery socket polarity; unplug USB
   and pair `KEEB Wireless` from the computer's Bluetooth settings.

The knob changes volume; pushing it mutes. The two added keys are Print Screen
and Pause and can be remapped in Vial through USB.
Fn is the key immediately right of Right Alt.
Fn+1/2/3 select Bluetooth profiles 1/2/3. Hold a profile shortcut for five
seconds to clear its bond and pair another host. Fn+B toggles USB/Bluetooth output.

## Edit or rebuild

Build from this folder. `src/board.rs` and the pinned dependency versions are required.

Install current Rust from https://rustup.rs/ and run in this folder:

```powershell
rustup target add thumbv6m-none-eabi
cargo install elf2uf2-rs
cargo install flip-link
cargo build --release --locked
elf2uf2-rs target/thumbv6m-none-eabi/release/keeb-rmk KEEB.uf2
```

`keyboard.toml` defines the actual electrical keymap, two layers and encoder.
Electrical matrix rows differ from physical key rows: preserve the cell mapping.
`vial.json` defines the physical layout for Vial.
`src/board.rs` controls charging, OLED and RGB.
The project includes the vendor radio firmware and its license.

## Board behavior

- GPIO0–5: row inputs; GPIO6–20: column outputs, column-to-row diodes.
- GPIO27/28: encoder A/B. Encoder push: matrix cell (0,14).
- GPIO21: software-I2C SCL; GPIO22: software-I2C SDA. These are not a hardware
  I2C pair, so do not replace the software driver with a normal I2C initializer.
- PCA9536 at 0x41: P0 RGB enable, P1 charger enable, P2 active-low USB presence,
  P3 active-low RGB fault. Output latches are cleared before enabling outputs.
- Charging and RGB enable only with USB physically present AND configured by
  a USB host. A charge-only wall adapter is not supported by this conservative
  policy; there is no charger-port detection circuit. Charging current is set
  by the hardware 10 kOhm resistor, approximately 100 mA.
- RGB is static dim blue (channel value 8/255), USB only. Estimated LED channel
  current is approximately 50 mA using a conservative 20 mA/channel model;
  measure actual total before increasing brightness. USB removal turns RGB off.
  An RGB fault latches RGB off until USB is removed.
- GPIO26/PIO1/DMA3 drive the 79 LEDs. Radio uses PIO0/DMA0/DMA2; flash uses DMA1.
- OLED at 0x3C: SSD1306 128x32, portrait orientation, KEEB plus USB/BAT status.
  BAT means battery mode, not a measured state of charge. Battery percentage and
  actual charging-complete status are not available from this PCB.

## Validation boundary

Release build and link passed for RP2040. Keymap was generated from all 80
switches and the encoder-push diode in the published PCB, with no duplicate
electrical cells. Operation on an assembled keyboard has not been verified.
Typing, BLE pairing/reconnection, encoder resolution/direction, display
orientation, current draw, USB suspend behavior and charging must be checked on
the real board. This is firmware for initial hardware testing, not a claim of
full operating or USB compliance testing.

Official RMK example: https://github.com/rmk-rs/rmk/tree/main/examples/use_config/pi_pico_w_ble
