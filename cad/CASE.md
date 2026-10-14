# Editable case

[Onshape editable case](https://cad.onshape.com/documents/a273834a75c8cf83eaf3eccf/w/175ac2f63e3c24ffd6632dd1/e/0ae0e2bf9948a84417651b67)

[Onshape assembly](https://cad.onshape.com/documents/a273834a75c8cf83eaf3eccf/w/175ac2f63e3c24ffd6632dd1/e/81324d641242e8976fe8d2e8)

`KEEB-case.fs` is the repaired FeatureScript used by the existing Finish KEEB tray feature. It depends on the original tray and PCB reference selected in that document; it is not a standalone assembly export. Use the linked document to edit or export the native case. A local STEP export was deferred at the user's request.

The repaired feature preserves 80 key centers and the evenly spaced 15-key top row. It produces the bottom, top, modeled feet and removable battery cover. The BOM specifies printing the bottom, top and battery cover and using adhesive feet.

The native feature regenerated successfully in the previous CAD check. Its assembly interference check against the earlier modeled PCB reported no interference. The latest populated PCB and newly selected purchased parts still need exact fit confirmation; see the root `HARDWARE.md`.
