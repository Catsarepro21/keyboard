# KEEB

80-key Pico W keyboard with Bluetooth and USB, a volume encoder, OLED and USB-only RGB.

- [BOM and purchasing links](BOM.md) · [CSV](BOM.csv)
- [Case STEP with integrated switch plate](cad/KEEB-case.step)
- [KiCad project](pcb/keeb_pcb.kicad_pro)
- [Gerbers and drill files](fabrication/KEEB-gerbers.zip)
- [Firmware source and build instructions](firmware/README.md) · [UF2](firmware/KEEB.uf2)

The case STEP contains the bottom, top with switch plate, and battery cover. Print the three case parts; no separate purchased plate is needed.

Release compilation passed. PCB ERC, DRC and schematic parity checks passed. The case solids are valid and do not intersect each other. Actual assembly, keycap/stabilizer fit and firmware operation on the assembled keyboard still need physical testing.
