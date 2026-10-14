FeatureScript 3083;
import(path : "onshape/std/common.fs", version : "3083.0");

// Completes the existing tray and a separate full top half for standard Cherry MX.
// Switch centers and stabilizer clearances follow the linked PCB geometry.
annotation { "Feature Type Name" : "Finish KEEB tray" }
export const finishKeeb = defineFeature(function(context is Context, id is Id, definition is map)
precondition
{
    annotation { "Name" : "Existing bottom tray", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
    definition.tray is Query;
    annotation { "Name" : "Linked PCB reference", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
    definition.pcb is Query;
    annotation { "Name" : "PCB side clearance" }
    isLength(definition.clearance, { (millimeter) : [0.3, 0.6, 3] } as LengthBoundSpec);
    annotation { "Name" : "Standoff height above floor" }
    isLength(definition.height, { (millimeter) : [8.5, 9.5, 15] } as LengthBoundSpec);
    annotation { "Name" : "Keycap opening width (1u)" }
    isLength(definition.switchOpening, { (millimeter) : [18.2, 18.8, 18.9] } as LengthBoundSpec);
    annotation { "Name" : "Heat-set pocket diameter" }
    isLength(definition.insertPocket, { (millimeter) : [4.2, 4.4, 4.8] } as LengthBoundSpec);
    annotation { "Name" : "Heat-set pocket depth" }
    isLength(definition.insertDepth, { (millimeter) : [4, 4.5, 6] } as LengthBoundSpec);
    annotation { "Name" : "Plate top above PCB top" }
    isLength(definition.plateHeight, { (millimeter) : [4, 5.5, 8] } as LengthBoundSpec);
}
{
    const trayBox = evBox3d(context, { "topology" : definition.tray, "tight" : true });
    const boardBox = evBox3d(context, { "topology" : definition.pcb, "tight" : true });
    const boardSize = boardBox.maxCorner - boardBox.minCorner;
    if (abs(boardSize[0] - 359.95 * millimeter) > 0.15 * millimeter || abs(boardSize[1] - 124.25 * millimeter) > 0.15 * millimeter)
        throw regenError("PCB outline differs from the verified KEEB layout. Recheck the mounting candidates.", ["pcb"]);
    const center = (trayBox.maxCorner + trayBox.minCorner) / 2;
    const boardCenter = (boardBox.maxCorner + boardBox.minCorner) / 2;
    const floorZ = trayBox.minCorner[2] + 4 * millimeter;
    const boardZ = floorZ + definition.height;
    const delta = vector(center[0] - boardCenter[0], center[1] - boardCenter[1], boardZ - boardBox.minCorner[2]);
    opTransform(context, id + "positionPCB", { "bodies" : definition.pcb, "transform" : transform(delta) });

    // Expand both the outside and cavity while retaining the original 3 mm walls.
    const neededX = max(0 * millimeter, (boardSize[0] + 2 * definition.clearance - (trayBox.maxCorner[0] - trayBox.minCorner[0] - 6 * millimeter)) / 2);
    const neededY = max(0 * millimeter, (boardSize[1] + 2 * definition.clearance - (trayBox.maxCorner[1] - trayBox.minCorner[1] - 6 * millimeter)) / 2);
    var outsideX = []; var insideX = []; var outsideY = []; var insideY = [];
    for (var face in evaluateQuery(context, qOwnedByBody(definition.tray, EntityType.FACE)))
    {
        const b = evBox3d(context, { "topology" : face, "tight" : true });
        if (b.maxCorner[0] - b.minCorner[0] < 0.001 * millimeter)
        {
            if (abs(b.minCorner[0] - trayBox.minCorner[0]) < 0.01 * millimeter || abs(b.minCorner[0] - trayBox.maxCorner[0]) < 0.01 * millimeter)
                outsideX = append(outsideX, face);
            else insideX = append(insideX, face);
        }
        if (b.maxCorner[1] - b.minCorner[1] < 0.001 * millimeter)
        {
            if (abs(b.minCorner[1] - trayBox.minCorner[1]) < 0.01 * millimeter || abs(b.minCorner[1] - trayBox.maxCorner[1]) < 0.01 * millimeter)
                outsideY = append(outsideY, face);
            else insideY = append(insideY, face);
        }
    }
    if (neededX > 0 * millimeter)
    {
        opOffsetFace(context, id + "outsideX", { "moveFaces" : qUnion(outsideX), "offsetDistance" : neededX });
        opOffsetFace(context, id + "insideX", { "moveFaces" : qUnion(insideX), "offsetDistance" : -neededX });
    }
    if (neededY > 0 * millimeter)
    {
        opOffsetFace(context, id + "outsideY", { "moveFaces" : qUnion(outsideY), "offsetDistance" : neededY });
        opOffsetFace(context, id + "insideY", { "moveFaces" : qUnion(insideY), "offsetDistance" : -neededY });
    }

    // The ten paired large/small holes are PCB stabilizer mounts, not case screws.
    const candidates = [[25.00375, -124.455], [48.81625, -124.455], [96.56, -143.55], [196.5725, -143.55], [263.13375, -124.455], [267.88375, -105.405], [270.27375, -67.305], [286.94625, -124.455], [291.69625, -105.405], [294.08625, -67.305]];
    const cylinders = qGeometry(qOwnedByBody(definition.pcb, EntityType.FACE), GeometryType.CYLINDER);
    var supports = [];
    for (var side = 0; side < 2; side += 1)
    {
        const bx = center[0] + (side == 0 ? -boardSize[0]/2 : boardSize[0]/2);
        const sx1 = side == 0 ? bx - definition.clearance - 0.1 * millimeter : bx - 1 * millimeter;
        const sx2 = side == 0 ? bx + 1 * millimeter : bx + definition.clearance + 0.1 * millimeter;
        const supportId = id + ("edgeSupport" ~ side);
        fCuboid(context, supportId, { "corner1" : vector(sx1, center[1] - boardSize[1]/2 - definition.clearance, floorZ - 0.1 * millimeter), "corner2" : vector(sx2, center[1] + boardSize[1]/2 + definition.clearance, boardZ) });
        supports = append(supports, qCreatedBy(supportId, EntityType.BODY));
    }
    opBoolean(context, id + "mergeEdgeSupports", { "tools" : qUnion([definition.tray, qUnion(supports)]), "operationType" : BooleanOperationType.UNION });
    setProperty(context, { "entities" : definition.tray, "propertyType" : PropertyType.NAME, "value" : "KEEB bottom case" });
    setProperty(context, { "entities" : definition.pcb, "propertyType" : PropertyType.NAME, "value" : "PCB reference - do not print" });


    // Rear opening follows the populated Pico controller position.
    const finalBox = evBox3d(context, { "topology" : definition.tray, "tight" : true });
    const rimZ = finalBox.maxCorner[2];
    const portX = 314.25 * millimeter + delta[0];
    fCuboid(context, id + "cableRelief", { "corner1" : vector(portX - 18 * millimeter, finalBox.maxCorner[1] - 4 * millimeter, boardZ - 1 * millimeter), "corner2" : vector(portX + 18 * millimeter, finalBox.maxCorner[1] + 2 * millimeter, rimZ + 4 * millimeter) });
    opBoolean(context, id + "openCable", { "targets" : definition.tray, "tools" : qCreatedBy(id + "cableRelief", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });

    // Full top cover with keycap-sized openings; the switches are PCB mounted.
    const carrierThickness = 1.6 * millimeter;
    const plateZ = boardZ + carrierThickness + definition.plateHeight;
    const plateBottom = plateZ - 3 * millimeter;
    fCuboid(context, id + "bezelBlank", { "corner1" : vector(finalBox.minCorner[0] - 1.5 * millimeter, finalBox.minCorner[1] - 1.5 * millimeter, rimZ), "corner2" : vector(finalBox.maxCorner[0] + 1.5 * millimeter, finalBox.maxCorner[1] + 1.5 * millimeter, plateZ) });
    const bezel = qCreatedBy(id + "bezelBlank", EntityType.BODY);
    fCuboid(context, id + "underPlateCavity", { "corner1" : vector(center[0] - boardSize[0]/2 - definition.clearance, center[1] - boardSize[1]/2 - definition.clearance, rimZ - 0.1 * millimeter), "corner2" : vector(center[0] + boardSize[0]/2 + definition.clearance, center[1] + boardSize[1]/2 + definition.clearance, plateBottom) });
    opBoolean(context, id + "hollowTop", { "targets" : bezel, "tools" : qCreatedBy(id + "underPlateCavity", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
    // Exact KiCad switch centers. Long-key units follow row spacing and stabilizer layout.
    const keyLayout = [[25.0,-40,1],[44.05,-40,1],[63.1,-40,1],[82.15,-40,1],[101.2,-40,1],[120.25,-40,1],[139.3,-40,1],[158.35,-40,1],[177.4,-40,1],[196.45,-40,1],[215.5,-40,1],[234.55,-40,1],[253.6,-40,1],[272.65,-40,1],[291.7,-40,1],[25,-59.05,1],[44.05,-59.05,1],[63.1,-59.05,1],[82.15,-59.05,1],[101.2,-59.05,1],[120.25,-59.05,1],[139.3,-59.05,1],[158.35,-59.05,1],[177.4,-59.05,1],[196.45,-59.05,1],[215.5,-59.05,1],[234.55,-59.05,1],[253.6,-59.05,1],[282.18,-59.05,2],[29.76,-78.1,1.5],[53.58,-78.1,1],[72.63,-78.1,1],[91.68,-78.1,1],[110.73,-78.1,1],[129.78,-78.1,1],[148.83,-78.1,1],[167.88,-78.1,1],[186.93,-78.1,1],[205.98,-78.1,1],[225.03,-78.1,1],[244.08,-78.1,1],[263.13,-78.1,1],[286.94,-78.1,1.5],[32.14,-97.15,1.75],[58.33,-97.15,1],[77.38,-97.15,1],[96.43,-97.15,1],[115.48,-97.15,1],[134.53,-97.15,1],[153.58,-97.15,1],[172.63,-97.15,1],[191.68,-97.15,1],[210.73,-97.15,1],[229.78,-97.15,1],[248.83,-97.15,1],[279.79,-97.15,2.25],[36.91,-116.2,2.25],[67.87,-116.2,1],[86.92,-116.2,1],[105.97,-116.2,1],[125.02,-116.2,1],[144.07,-116.2,1],[163.12,-116.2,1],[182.17,-116.2,1],[201.22,-116.2,1],[220.27,-116.2,1],[239.32,-116.2,1],[275.04,-116.2,2.75],[336.5,-116.2,1],[27.38,-135.25,1.25],[51.19,-135.25,1.25],[75,-135.25,1.25],[146.44,-135.25,6.25],[217.88,-135.25,1.25],[241.69,-135.25,1.25],[265.5,-135.25,1.25],[289.31,-135.25,1.25],[317.4,-135.25,1],[336.45,-135.25,1],[355.5,-135.25,1]];
    // Key centers were verified against the updated 80-switch KiCad/STEP layout.
    // The retained construction PCB supplies the unchanged outline and stabilizer mounts.
    var openings = []; var switchCount = 0; var stabilizerCount = 0;
    for (var face in evaluateQuery(context, cylinders))
    {
        const c = evSurfaceDefinition(context, { "face" : face });
        if (abs(c.radius - 1.9939 * millimeter) > 0.03 * millimeter) continue;
        const p = c.coordSystem.origin;
        for (var candidate in candidates)
        {
            const desired = vector(candidate[0], candidate[1], 0) * millimeter + delta;
            if (norm(vector(p[0]-desired[0], p[1]-desired[1], 0 * millimeter)) < 0.15 * millimeter) stabilizerCount += 1;
        }
    }
    for (var key in keyLayout)
    {
        const p = vector(key[0],key[1],0)*millimeter+delta;
        const cutId = id + ("keycapOpening" ~ switchCount);
        const halfX = (definition.switchOpening + (key[2] - 1) * 19.05 * millimeter) / 2;
        const halfY = definition.switchOpening / 2;
        fCuboid(context, cutId, { "corner1" : vector(p[0]-halfX,p[1]-halfY,rimZ-0.1*millimeter), "corner2" : vector(p[0]+halfX,p[1]+halfY,plateZ+0.1*millimeter) });
        openings = append(openings, qCreatedBy(cutId, EntityType.BODY));
        switchCount += 1;
    }
    if (switchCount != 80 || stabilizerCount != 10) throw regenError("Unexpected key layout or stabilizer mounting pattern.");
    opBoolean(context, id + "cutKeycapOpenings", { "targets" : bezel, "tools" : qUnion(openings), "operationType" : BooleanOperationType.SUBTRACTION });
    println("Keycap openings: " ~ switchCount ~ "; KiCad layout with 6.25u spacebar.");
    // Open key fields remove fragile 0.25 mm webs without narrowing keycap travel.
    // Bounds allow 19.0 mm 1u cap envelopes, with at least 0.29 mm edge allowance.
    const keyFields = [[15.15,-145.10,301.55,-30.15],[307.55,-145.10,365.35,-125.40],[326.65,-126.05,346.35,-106.35]];
    for (var ki=0;ki<size(keyFields);ki+=1)
    {
        const k=keyFields[ki]; const fid=id+("openKeyField"~ki);
        fCuboid(context,fid,{"corner1":vector(k[0]*millimeter+delta[0],k[1]*millimeter+delta[1],rimZ-0.1*millimeter),"corner2":vector(k[2]*millimeter+delta[0],k[3]*millimeter+delta[1],plateZ+0.1*millimeter)});
        opBoolean(context,fid+"cut",{"targets":bezel,"tools":qCreatedBy(fid,EntityType.BODY),"operationType":BooleanOperationType.SUBTRACTION});
    }

    fCuboid(context, id + "bezelPort", { "corner1" : vector(portX - 18 * millimeter, center[1] + boardSize[1] / 2 - 2 * millimeter, rimZ - 0.1 * millimeter), "corner2" : vector(portX + 18 * millimeter, finalBox.maxCorner[1] + 2 * millimeter, plateZ + 0.1 * millimeter) });
    opBoolean(context, id + "openBezelPort", { "targets" : bezel, "tools" : qCreatedBy(id + "bezelPort", EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
    // Flat roof; bare Pico is soldered directly to the common carrier footprint.
    const controlTop = plateZ;
    const oledRise = 8 * millimeter;
    const oledTop = plateZ + oledRise;
    // Compact display surround; all other cover surfaces retain the flat profile.
    fCuboid(context,id+"raisedOled", { "corner1" : vector(332.3*millimeter+delta[0],-91.2*millimeter+delta[1],plateZ-0.2*millimeter), "corner2" : vector(352*millimeter+delta[0],-46.2*millimeter+delta[1],oledTop) });
    const oledSurround = qCreatedBy(id+"raisedOled",EntityType.BODY);
    var oledCorners = [];
    for (var e in evaluateQuery(context,qOwnedByBody(oledSurround,EntityType.EDGE)))
    {
        const b = evBox3d(context,{"topology":e,"tight":true});
        if (b.maxCorner[2]-b.minCorner[2]>5*millimeter) oledCorners=append(oledCorners,e);
    }
    opFillet(context,id+"roundOledCorners",{"entities":qUnion(oledCorners),"radius":2*millimeter});
    var oledRim=[];
    for (var e in evaluateQuery(context,qOwnedByBody(oledSurround,EntityType.EDGE)))
    {
        const b=evBox3d(context,{"topology":e,"tight":true});
        if (abs(b.minCorner[2]-oledTop)<0.001*millimeter && abs(b.maxCorner[2]-oledTop)<0.001*millimeter) oledRim=append(oledRim,e);
    }
    opFillet(context,id+"softOledRim",{"entities":qUnion(oledRim),"radius":0.8*millimeter});
    opBoolean(context,id+"joinRaisedOled",{"tools":qUnion([bezel,oledSurround]),"operationType":BooleanOperationType.UNION});
    fCuboid(context,id+"picoUnderPocket", { "corner1" : vector(302.925*millimeter+delta[0],-77.97*millimeter+delta[1],rimZ-0.1*millimeter), "corner2" : vector(324.925*millimeter+delta[0],-25.025*millimeter+delta[1],boardZ+5.795*millimeter) });
    opBoolean(context,id+"clearBarePico", { "targets" : bezel, "tools" : qCreatedBy(id+"picoUnderPocket",EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
    // Display is wired to J1 and held at the cover surface, not on a tall header.
    // Clearance under the surrounding OLED module and its connector area.
    fCuboid(context,id+"oledUnderPocket", { "corner1" : vector(334.8*millimeter+delta[0],-89.2*millimeter+delta[1],rimZ-0.1*millimeter), "corner2" : vector(349.4*millimeter+delta[0],-45.167*millimeter+delta[1],oledTop-1.3*millimeter) });
    opBoolean(context,id+"clearWiredOled", { "targets" : bezel, "tools" : qCreatedBy(id+"oledUnderPocket",EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
    // Window follows the imported display body with 0.5 mm per side.
    fCuboid(context,id+"displayWindow", { "corner1" : vector(335.57*millimeter+delta[0],-88.4*millimeter+delta[1],rimZ-0.1*millimeter), "corner2" : vector(348.67*millimeter+delta[0],-49.1*millimeter+delta[1],oledTop+0.1*millimeter) });
    // Encoder housing projects above this cover; retain clearance for it.
    fCuboid(context,id+"encoderWindow", { "corner1" : vector(329.2*millimeter+delta[0],-44.7*millimeter+delta[1],rimZ-0.1*millimeter), "corner2" : vector(342.5*millimeter+delta[0],-31.3*millimeter+delta[1],controlTop+0.1*millimeter) });
    opBoolean(context,id+"openControls", { "targets" : bezel, "tools" : qUnion([qCreatedBy(id+"displayWindow",EntityType.BODY),qCreatedBy(id+"encoderWindow",EntityType.BODY)]), "operationType" : BooleanOperationType.SUBTRACTION });
    // Two end shelves support the raised wired OLED PCB. A 0.1 mm adhesive layer
    // between shelf and module places its modeled top flush with the roof.
    for (var rail = 0; rail < 2; rail += 1)
    {
        const ry = rail == 0 ? -88.3 * millimeter : -53.5 * millimeter;
        const rid = id + ("oledShelf" ~ rail);
        fCuboid(context,rid,{ "corner1" : vector(334.3*millimeter+delta[0],ry+delta[1],boardZ+10.935*millimeter), "corner2" : vector(349.9*millimeter+delta[0],ry+2*millimeter+delta[1],boardZ+11.935*millimeter) });
        opBoolean(context,id+("joinOledShelf" ~ rail),{ "tools" : qUnion([bezel,qCreatedBy(rid,EntityType.BODY)]), "operationType" : BooleanOperationType.UNION });
    }
    // Internal cable descent beside the OLED, without a visible external notch.
    fCuboid(context,id+"batteryWireChannel",{"corner1":vector(348.4*millimeter+delta[0],-85.5*millimeter+delta[1],rimZ-0.1*millimeter),"corner2":vector(350.4*millimeter+delta[0],-78*millimeter+delta[1],boardZ+12*millimeter)});
    opBoolean(context,id+"clearBatteryWireChannel",{"targets":bezel,"tools":qCreatedBy(id+"batteryWireChannel",EntityType.BODY),"operationType":BooleanOperationType.SUBTRACTION});
    // Maintained slide-switch access, with 0.4 mm side clearance around its body.
    fCuboid(context,id+"powerSwitchAccess",{"corner1":vector(357.6*millimeter+delta[0],-66.65*millimeter+delta[1],rimZ-0.1*millimeter),"corner2":vector(368.4*millimeter+delta[0],-63.35*millimeter+delta[1],plateZ+0.1*millimeter)});
    opBoolean(context,id+"openPowerSwitch",{"targets":bezel,"tools":qCreatedBy(id+"powerSwitchAccess",EntityType.BODY),"operationType":BooleanOperationType.SUBTRACTION});
    // Cable access remains on the rear wall below the solid control roof.
    fCuboid(context,id+"controlCableAccess", { "corner1" : vector(portX-18*millimeter,center[1]+boardSize[1]/2-4.1*millimeter,rimZ-0.1*millimeter), "corner2" : vector(portX+18*millimeter,finalBox.maxCorner[1]+2*millimeter,controlTop-3*millimeter) });
    opBoolean(context,id+"controlCableCut", { "targets" : bezel, "tools" : qCreatedBy(id+"controlCableAccess",EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
    // All four contact faces and pad pockets are parallel to the desk at the typing angle.
    const footSpan = finalBox.maxCorner[1] - finalBox.minCorner[1] - 30 * millimeter;
    const footAngle = atan(10 * millimeter / footSpan);
    const footNormal = vector(0, sin(footAngle), cos(footAngle));
    const footRotation = matrix([[1,0,0],[0,cos(footAngle),sin(footAngle)],[0,-sin(footAngle),cos(footAngle)]]);
    for (var footX = 0; footX < 2; footX += 1)
    {
        for (var footY = 0; footY < 2; footY += 1)
        {
            const fx = footX == 0 ? finalBox.minCorner[0] + 20 * millimeter : finalBox.maxCorner[0] - 20 * millimeter;
            const fy = footY == 0 ? finalBox.minCorner[1] + 10 * millimeter : finalBox.maxCorner[1] - 20 * millimeter;
            const footId = id + ("foot" ~ footX ~ footY);
            const footBase = finalBox.minCorner[2] - (footY == 1 ? 12 : 2) * millimeter;
            const footCenter = vector(fx,fy,footBase);
            const riserId = id + ("angledRiser" ~ footX ~ footY);
            fCylinder(context,riserId,{ "bottomCenter" : footCenter-vector(0,0,2)*millimeter, "topCenter" : vector(fx,fy,finalBox.minCorner[2]+0.2*millimeter), "radius" : 9*millimeter });
            const trimId = id + ("footTrim" ~ footX ~ footY);
            fCuboid(context,trimId,{ "corner1" : vector(-14,-14,-30)*millimeter, "corner2" : vector(14,14,0)*millimeter });
            opTransform(context,trimId+"orient",{ "bodies" : qCreatedBy(trimId,EntityType.BODY), "transform" : transform(footRotation,footCenter) });
            opBoolean(context,trimId+"cut",{ "targets" : qCreatedBy(riserId,EntityType.BODY), "tools" : qCreatedBy(trimId,EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
            opBoolean(context,id+("joinAngledFoot" ~ footX ~ footY),{ "tools" : qUnion([definition.tray,qCreatedBy(riserId,EntityType.BODY)]), "operationType" : BooleanOperationType.UNION });
            fCylinder(context,footId,{ "bottomCenter" : footCenter-0.1*millimeter*footNormal, "topCenter" : footCenter+1*millimeter*footNormal, "radius" : 6*millimeter });
            opBoolean(context,footId+"pocket",{ "targets" : definition.tray, "tools" : qCreatedBy(footId,EntityType.BODY), "operationType" : BooleanOperationType.SUBTRACTION });
        }
    }
    setProperty(context, { "entities" : bezel, "propertyType" : PropertyType.NAME, "value" : "KEEB top cover - keycap openings" });


    for (var shell in [definition.tray, bezel])
    {
        const shellBox = evBox3d(context, { "topology" : shell, "tight" : true });
        var corners = [];
        for (var edge in evaluateQuery(context, qOwnedByBody(shell, EntityType.EDGE)))
        {
            const eb = evBox3d(context, { "topology" : edge, "tight" : true });
            if (eb.maxCorner[0] - eb.minCorner[0] < 0.001 * millimeter && eb.maxCorner[1] - eb.minCorner[1] < 0.001 * millimeter && eb.maxCorner[2] - eb.minCorner[2] > 2 * millimeter &&
                (abs(eb.minCorner[0] - shellBox.minCorner[0]) < 0.001 * millimeter || abs(eb.minCorner[0] - shellBox.maxCorner[0]) < 0.001 * millimeter) &&
                (abs(eb.minCorner[1] - shellBox.minCorner[1]) < 0.001 * millimeter || abs(eb.minCorner[1] - shellBox.maxCorner[1]) < 0.001 * millimeter)) corners = append(corners, edge);
        }
        if (size(corners) > 0) opFillet(context, id + (shell == bezel ? "bezelCorners" : "trayCorners"), { "entities" : qUnion(corners), "radius" : 1.5 * millimeter });
    }
    // Round the outer upper rim without touching the narrow key dividers.
    var upperRimEdges = [];
    const topBox = evBox3d(context, { "topology" : bezel, "tight" : true });
    for (var edge in evaluateQuery(context, qOwnedByBody(bezel, EntityType.EDGE)))
    {
        const eb = evBox3d(context, { "topology" : edge, "tight" : true });
        if (abs(eb.minCorner[2] - plateZ) < 0.001 * millimeter && abs(eb.maxCorner[2] - plateZ) < 0.001 * millimeter &&
            (abs(eb.minCorner[0] - topBox.minCorner[0]) < 0.001 * millimeter || abs(eb.maxCorner[0] - topBox.maxCorner[0]) < 0.001 * millimeter || abs(eb.minCorner[1] - topBox.minCorner[1]) < 0.001 * millimeter || abs(eb.maxCorner[1] - topBox.maxCorner[1]) < 0.001 * millimeter)) upperRimEdges = append(upperRimEdges, edge);
    }
    if (size(upperRimEdges) == 0) throw regenError("No outside upper rim found for the comfort fillet.");
    opFillet(context, id + "comfortRim", { "entities" : qUnion(upperRimEdges), "radius" : 1.2 * millimeter });
    // Reinforced external tabs keep the 5 mm OD inserts clear of the PCB.
    for (var row = 0; row < 2; row += 1)
    {
        const y = center[1] + (row == 0 ? -1 : 1) * (boardSize[1]/2 + 5.7 * millimeter);
        for (var col = 0; col < 4; col += 1)
        {
            const x = center[0] + (-150 + col * 100) * millimeter;
            const suffix = "r" ~ row ~ "c" ~ col;
            const lowerId = id + ("insertTab" ~ suffix);
            fCylinder(context, lowerId, { "bottomCenter" : vector(x,y,finalBox.minCorner[2]), "topCenter" : vector(x,y,rimZ), "radius" : 5 * millimeter });
            opBoolean(context, id + ("joinLowerTab" ~ suffix), { "tools" : qUnion([definition.tray,qCreatedBy(lowerId,EntityType.BODY)]), "operationType" : BooleanOperationType.UNION });
            const upperId = id + ("coverTab" ~ suffix);
            fCylinder(context, upperId, { "bottomCenter" : vector(x,y,rimZ), "topCenter" : vector(x,y,plateZ), "radius" : 5 * millimeter });
            var tabEdges = [];
            for (var edge in evaluateQuery(context,qCreatedBy(upperId,EntityType.EDGE)))
            {
                const eb = evBox3d(context,{ "topology" : edge,"tight" : true });
                if (abs(eb.minCorner[2]-plateZ)<0.001*millimeter && abs(eb.maxCorner[2]-plateZ)<0.001*millimeter) tabEdges=append(tabEdges,edge);
            }
            opFillet(context,id + ("roundTab" ~ suffix),{ "entities" : qUnion(tabEdges),"radius" : 1.2*millimeter });
            opBoolean(context, id + ("joinUpperTab" ~ suffix), { "tools" : qUnion([bezel,qCreatedBy(upperId,EntityType.BODY)]), "operationType" : BooleanOperationType.UNION });
            fCylinder(context,id + ("coverBore" ~ suffix),{ "bottomCenter" : vector(x,y,rimZ-0.1*millimeter),"topCenter" : vector(x,y,plateZ+0.1*millimeter),"radius" : 1.7*millimeter });
            opBoolean(context,id + ("coverDrill" ~ suffix),{ "targets" : bezel,"tools" : qCreatedBy(id + ("coverBore" ~ suffix),EntityType.BODY),"operationType" : BooleanOperationType.SUBTRACTION });
            fCylinder(context,id + ("lowerBore" ~ suffix),{ "bottomCenter" : vector(x,y,finalBox.minCorner[2]-0.1*millimeter),"topCenter" : vector(x,y,rimZ+0.1*millimeter),"radius" : 1.7*millimeter });
            fCylinder(context,id + ("insertPocket" ~ suffix),{ "bottomCenter" : vector(x,y,rimZ-definition.insertDepth),"topCenter" : vector(x,y,rimZ+0.1*millimeter),"radius" : definition.insertPocket/2 });
            opBoolean(context,id + ("lowerDrill" ~ suffix),{ "targets" : definition.tray,"tools" : qUnion([qCreatedBy(id + ("lowerBore" ~ suffix),EntityType.BODY),qCreatedBy(id + ("insertPocket" ~ suffix),EntityType.BODY)]),"operationType" : BooleanOperationType.SUBTRACTION });
        }
    }
    // Eight stepped through-holes below, eight full through-holes above.
    for (var shell in [definition.tray,bezel])
    {
        var boreCount=0; var pocketCount=0;
        for (var face in evaluateQuery(context,qGeometry(qOwnedByBody(shell,EntityType.FACE),GeometryType.CYLINDER)))
        {
            const c=evSurfaceDefinition(context,{ "face" : face });
            const fb=evBox3d(context,{ "topology" : face,"tight" : true });
            if (abs(c.radius-1.7*millimeter)<0.001*millimeter && abs(fb.minCorner[2]-(shell==bezel?rimZ:finalBox.minCorner[2]))<0.001*millimeter && abs(fb.maxCorner[2]-(shell==bezel?plateZ:rimZ-definition.insertDepth))<0.001*millimeter) boreCount+=1;
            if (shell==definition.tray && abs(c.radius-definition.insertPocket/2)<0.001*millimeter && abs(fb.minCorner[2]-(rimZ-definition.insertDepth))<0.001*millimeter && abs(fb.maxCorner[2]-rimZ)<0.001*millimeter) pocketCount+=1;
        }
        if (boreCount!=8 || (shell==definition.tray && pocketCount!=8)) throw regenError("Expected eight M3 through-bores and eight heat-set pockets in the bottom.");
        println((shell==bezel?"Top":"Bottom") ~ " verified M3 bores: " ~ boreCount ~ "; insert pockets: " ~ pocketCount);
    }

    // Positive edge capture: 0.20 mm above the real 1.6 mm carrier.
    // Clips are outside the switch fields and contact only clear PCB edge strips.
    const clipStations = [[11.225,-78.1,0],[11.225,-116.2,0],[371.175,-100,1],[371.175,-140,1]];
    for (var ci=0;ci<size(clipStations);ci+=1)
    {
        const c=clipStations[ci]; const lipid=id+("pcbRetain"~ci);
        const x1=(c[2]==0?c[0]-1:c[0]-1.25)*millimeter+delta[0];
        const x2=(c[2]==0?c[0]+1.25:c[0]+1)*millimeter+delta[0];
        fCuboid(context,lipid,{"corner1":vector(x1,(c[1]-3)*millimeter+delta[1],boardZ+carrierThickness+0.20*millimeter),"corner2":vector(x2,(c[1]+3)*millimeter+delta[1],plateZ)});
        opBoolean(context,lipid+"join",{"tools":qUnion([bezel,qCreatedBy(lipid,EntityType.BODY)]),"operationType":BooleanOperationType.UNION});
    }
    const pcbSupports = [[86.5,-91.0],[177.0,-74.0],[262.0,-97.0],[176.0,-144.0]];
    for (var pi=0;pi<size(pcbSupports);pi+=1)
    {
        const s=pcbSupports[pi]; const sid=id+("pcbCenterSupport"~pi);
        fCylinder(context,sid,{"bottomCenter":vector(s[0]*millimeter+delta[0],s[1]*millimeter+delta[1],floorZ-0.1*millimeter),"topCenter":vector(s[0]*millimeter+delta[0],s[1]*millimeter+delta[1],boardZ),"radius":2*millimeter});
        opBoolean(context,sid+"join",{"tools":qUnion([definition.tray,qCreatedBy(sid,EntityType.BODY)]),"operationType":BooleanOperationType.UNION});
    }
    // Protected 29 x 36 x 4.75 mm cell in a 31 x 38 mm pocket.
    // Lid reserves 0.70 mm above the cell including 0.05 mm insulation and 2.5 mm to PCB underside.
    const bx=349.5*millimeter+delta[0]; const by=-61*millimeter+delta[1];
    const bf=id+"batteryHolder";
    fCuboid(context,bf,{"corner1":vector(bx-17.5*millimeter,by-21*millimeter,floorZ-0.1*millimeter),"corner2":vector(bx+17.5*millimeter,by+21*millimeter,floorZ+5.5*millimeter)});
    fCuboid(context,id+"batteryPocket",{"corner1":vector(bx-15.5*millimeter,by-19*millimeter,floorZ),"corner2":vector(bx+15.5*millimeter,by+19*millimeter,floorZ+5.6*millimeter)});
    opBoolean(context,id+"batteryHollow",{"targets":qCreatedBy(bf,EntityType.BODY),"tools":qCreatedBy(id+"batteryPocket",EntityType.BODY),"operationType":BooleanOperationType.SUBTRACTION});
    opBoolean(context,id+"batteryJoin",{"tools":qUnion([definition.tray,qCreatedBy(bf,EntityType.BODY)]),"operationType":BooleanOperationType.UNION});
    fCuboid(context,id+"batteryWireExit",{"corner1":vector(bx-11.5*millimeter,by-21.1*millimeter,floorZ+1*millimeter),"corner2":vector(bx-5.5*millimeter,by-18.9*millimeter,floorZ+5.6*millimeter)});
    opBoolean(context,id+"batteryWireExitCut",{"targets":definition.tray,"tools":qCreatedBy(id+"batteryWireExit",EntityType.BODY),"operationType":BooleanOperationType.SUBTRACTION});
    const bl=id+"batteryRetentionLid";
    fCuboid(context,bl,{"corner1":vector(bx-17.5*millimeter,by-21*millimeter,floorZ+5.5*millimeter),"corner2":vector(bx+17.5*millimeter,by+21*millimeter,floorZ+7*millimeter)});
    const batteryLid=qCreatedBy(bl,EntityType.BODY);
    for(var li=0;li<2;li+=1)
    {
        const ly=by+(li==0?-24.5:24.5)*millimeter; const lidBoss=id+("batteryLidBoss"~li);
        fCylinder(context,lidBoss,{"bottomCenter":vector(bx,ly,floorZ+0.4*millimeter),"topCenter":vector(bx,ly,floorZ+7*millimeter),"radius":4.5*millimeter});
        opBoolean(context,lidBoss+"join",{"tools":qUnion([batteryLid,qCreatedBy(lidBoss,EntityType.BODY)]),"operationType":BooleanOperationType.UNION});
        fCylinder(context,lidBoss+"insert",{"bottomCenter":vector(bx,ly,floorZ+0.3*millimeter),"topCenter":vector(bx,ly,floorZ+4.9*millimeter),"radius":definition.insertPocket/2});
        fCylinder(context,lidBoss+"screwTip",{"bottomCenter":vector(bx,ly,floorZ+4.8*millimeter),"topCenter":vector(bx,ly,floorZ+5.5*millimeter),"radius":1.7*millimeter});
        opBoolean(context,lidBoss+"insertCut",{"targets":batteryLid,"tools":qUnion([qCreatedBy(lidBoss+"insert",EntityType.BODY),qCreatedBy(lidBoss+"screwTip",EntityType.BODY)]),"operationType":BooleanOperationType.SUBTRACTION});
        fCylinder(context,lidBoss+"floorBore",{"bottomCenter":vector(bx,ly,finalBox.minCorner[2]-0.1*millimeter),"topCenter":vector(bx,ly,floorZ+0.5*millimeter),"radius":1.7*millimeter});
        fCylinder(context,lidBoss+"headRelief",{"bottomCenter":vector(bx,ly,finalBox.minCorner[2]-0.1*millimeter),"topCenter":vector(bx,ly,finalBox.minCorner[2]+1*millimeter),"radius":3.1*millimeter});
        opBoolean(context,lidBoss+"floorCut",{"targets":definition.tray,"tools":qUnion([qCreatedBy(lidBoss+"floorBore",EntityType.BODY),qCreatedBy(lidBoss+"headRelief",EntityType.BODY)]),"operationType":BooleanOperationType.SUBTRACTION});
    }
    fCuboid(context,id+"batteryLidWireNotch",{"corner1":vector(bx-11.5*millimeter,by-21.1*millimeter,floorZ+5.4*millimeter),"corner2":vector(bx-5.5*millimeter,by-18.5*millimeter,floorZ+7.1*millimeter)});
    opBoolean(context,id+"batteryLidWireNotchCut",{"targets":batteryLid,"tools":qCreatedBy(id+"batteryLidWireNotch",EntityType.BODY),"operationType":BooleanOperationType.SUBTRACTION});
    setProperty(context,{"entities":batteryLid,"propertyType":PropertyType.NAME,"value":"KEEB removable battery cover"});

    opMateConnector(context, id + "boardSeat", { "coordSystem" : coordSystem(vector(center[0], center[1], boardZ), vector(1, 0, 0), vector(0, 0, 1)), "owner" : definition.tray, "attachTo" : definition.tray });
    setProperty(context, { "entities" : qCreatedBy(id + "boardSeat", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "PCB underside seat" });
    const seatCS = coordSystem(vector(center[0], finalBox.minCorner[1] - 8 * millimeter, rimZ), vector(1, 0, 0), vector(0, 0, 1));
    opMateConnector(context, id + "traySeat", { "coordSystem" : seatCS, "owner" : definition.tray, "attachTo" : definition.tray });
    opMateConnector(context, id + "bezelSeat", { "coordSystem" : seatCS, "owner" : bezel, "attachTo" : bezel });
    setProperty(context, { "entities" : qCreatedBy(id + "traySeat", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "Bezel seat - tray" });
    setProperty(context, { "entities" : qCreatedBy(id + "bezelSeat", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "Bezel seat - top" });


});

annotation { "Feature Type Name" : "KEEB PCB seating datum" }
export const keebDatum = defineFeature(function(context is Context, id is Id, definition is map)
precondition
{
    annotation { "Name" : "PCB", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
    definition.pcb is Query;
}
{
    const b = evBox3d(context, { "topology" : definition.pcb, "tight" : true });
    opMateConnector(context, id + "pcbSeat", { "coordSystem" : coordSystem(vector((b.minCorner[0] + b.maxCorner[0]) / 2, (b.minCorner[1] + b.maxCorner[1]) / 2, b.minCorner[2]), vector(1, 0, 0), vector(0, 0, 1)), "owner" : definition.pcb, "attachTo" : definition.pcb });
    setProperty(context, { "entities" : qCreatedBy(id + "pcbSeat", EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : "PCB underside datum" });
});



annotation { "Feature Type Name" : "Final PCB seating datum" }
export const finalPcbDatum = defineFeature(function(context is Context, id is Id, definition is map)
precondition
{
    annotation { "Name" : "Final PCB composite", "Filter" : EntityType.BODY, "MaxNumberOfPicks" : 1 }
    definition.pcb is Query;
}
{
    opMateConnector(context,id+"seatDatum",{ "coordSystem" : coordSystem(vector(191.2,-87.75,0)*millimeter,vector(1,0,0),vector(0,0,1)),"owner" : definition.pcb,"attachTo" : definition.pcb });
    setProperty(context,{ "entities" : qCreatedBy(id+"seatDatum",EntityType.BODY),"propertyType" : PropertyType.NAME,"value" : "Final PCB underside seat" });
}
);
