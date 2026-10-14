# Hardware checkpoint — 13 October 2026

Open `pcb/keeb_pcb.kicad_pro` with KiCad 10. Custom symbols, footprints and visible 3D models are included. Use the root `BOM.csv` and `BOM.md` for purchasing; the former lowercase BOM was obsolete.

- 357 footprints, 80 keys, one encoder with push, and 79 RGB LEDs.
- Fresh KiCad 10.0.6 ERC and DRC show zero violations, unconnected items and schematic parity issues, including all severities and all track errors. No ignored checks are reported.
- Packaging changed model paths, not nets, pad positions or routing. All 357 visible model entries resolve in the packaged project.
- OLED power is 3.3 V; RGB runs from USB only. Battery power passes through the switch and Schottky diode to Pico VSYS. Charging is set to 100 mA.
- `fabrication/KEEB-gerbers.zip` includes copper, solder mask, silkscreen, outline and separate plated/non-plated drills generated from this board. Order specifications: 359.95 x 124.25 mm, 2 layers, 1.6 mm FR4, green solder mask, 1 oz copper, HASL. The BOM estimate includes five boards.
- `cad/KEEB-case.fs` is the repaired Onshape feature source. See `cad/CASE.md` for the editable document and assembly.

## Remaining checks before ordering or submission

The new Fynmi stabilizers and white pudding caps are lower-cost sourcing candidates; exact mounting dimensions and keycap travel still need verification. Some models are generic or clearance envelopes. The existing Onshape assembly uses an earlier populated PCB reference; its earlier no-interference result does not establish fit of every part on the new board. Check OLED/harness support, USB plug access, battery/fastener/solder clearances and print tolerances in the complete assembly or a physical test.

Verify battery connector polarity (pin 1 BAT+, pin 2 GND) before connection. Operating current, charging behavior, RGB current limits and radio range require hardware testing. The power switch is rated 300 mA at 24 V DC. Firmware remains deferred and is not included. Other supplier shipping, tax and printing remain unquoted; $106.13 is a merchandise subtotal, not a delivered total.

This is a checked hardware checkpoint, not a complete manufacturing or Fabricate submission signoff. Complete firmware and exact-part assembly checks, and add your own project README and current journal entries before submission.
