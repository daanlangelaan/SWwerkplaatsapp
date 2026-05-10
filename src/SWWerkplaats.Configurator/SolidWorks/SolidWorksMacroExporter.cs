using System.Globalization;
using System.IO;
using System.Text;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.SolidWorks
{
    public sealed class SolidWorksMacroExporter
    {
        public string ExportMacro(WorkbenchModel model, string outputFolder)
        {
            var cadFolder = Path.Combine(outputFolder, "SolidWorks");
            Directory.CreateDirectory(cadFolder);

            var macroFileName = "Maak_" + SafeName(model.ProjectName) + "_Parts.bas";
            var macroPath = Path.Combine(outputFolder, macroFileName);
            File.WriteAllText(macroPath, BuildMacro(model, cadFolder), Encoding.Default);

            var instructionsPath = Path.Combine(outputFolder, "SolidWorksMacroInstructies.txt");
            File.WriteAllText(instructionsPath,
                "Gebruik bij SOLIDWORKS 3DEXPERIENCE als externe COM-koppeling niet beschikbaar is:\r\n\r\n" +
                "1. Open SOLIDWORKS via de normale 3DEXPERIENCE snelkoppeling.\r\n" +
                "2. Kies Tools > Macro > New.\r\n" +
                "3. Sla tijdelijk een macro op, bijvoorbeeld Tijdelijk.swp.\r\n" +
                "4. In de VBA editor: File > Import File en kies " + macroFileName + ".\r\n" +
                "5. Run Sub main.\r\n\r\n" +
                "Let op: run de nieuw geimporteerde module, niet een oude Macro1/module uit een vorige outputmap.\r\n\r\n" +
                "De parts en assembly worden opgeslagen in:\r\n" + cadFolder + "\r\n",
                Encoding.UTF8);

            return macroPath;
        }

        private static string BuildMacro(WorkbenchModel model, string cadFolder)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Option Explicit");
            sb.AppendLine("' GeneratorVersion: SWWerkplaats_CabinetVerticalZBaseline_2026_05_10");
            sb.AppendLine();
            sb.AppendLine("Dim swApp As Object");
            sb.AppendLine();
            sb.AppendLine("Sub main()");
            sb.AppendLine("    Set swApp = Application.SldWorks");
            sb.AppendLine("    Dim assemblyPath As String");
            sb.AppendLine("    assemblyPath = " + Q(Path.Combine(cadFolder, SafeName(model.ProjectName) + ".SLDASM")));

            foreach (var profile in model.Profiles)
            {
                var path = Path.Combine(cadFolder, SafeName(profile.Name) + "_" + profile.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
                sb.AppendLine("    CreateBoxPart " + Q(path) + ", " + M(ProfileX(profile)) + ", " + M(ProfileY(profile)) + ", " + M(ProfileZ(profile)) + ", " + Q(ProfileDrillData(profile)) + ", " + Q(ProfileAxis(profile)));
            }

            foreach (var sheet in model.Sheets)
            {
                var path = Path.Combine(cadFolder, SafeName(sheet.Name) + "_" + sheet.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "x" + sheet.WidthMm.ToString("0", CultureInfo.InvariantCulture) + ".SLDPRT");
                sb.AppendLine("    CreateSheetPart " + Q(path) + ", " + M(sheet.LengthMm) + ", " + M(sheet.Material.ThicknessMm) + ", " + M(sheet.WidthMm) + ", " + M(sheet.HasCornerNotches ? sheet.CornerNotchSizeMm : 0) + ", " + Q(HoleData(sheet)) + ", " + Q(SheetPartOrientation(model, sheet)) + ", " + M(sheet.HasToeKickNotch ? sheet.ToeKickDepthMm : 0) + ", " + M(sheet.HasToeKickNotch ? sheet.ToeKickHeightMm : 0));
            }

            AppendAssemblyCalls(sb, model, cadFolder);

            sb.AppendLine("    MsgBox \"Parts en assembly aangemaakt.\"");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateBoxPart(filePath As String, x As Double, y As Double, z As Double, drillData As String, lengthAxis As String)");
            sb.AppendLine("    Dim swModel As Object");
            sb.AppendLine("    Dim ok As Boolean");
            sb.AppendLine("    Dim errors As Long");
            sb.AppendLine("    Dim warnings As Long");
            sb.AppendLine("    Dim rectSegments As Variant");
            sb.AppendLine();
            sb.AppendLine("    Set swModel = swApp.NewPart");
            sb.AppendLine("    If swModel Is Nothing Then");
            sb.AppendLine("        MsgBox \"Kan geen nieuw part maken. Controleer de default part template in SOLIDWORKS.\"");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine();
            sb.AppendLine("    ok = swModel.Extension.SelectByID2(\"Front Plane\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("    If Not ok Then ok = swModel.Extension.SelectByID2(\"Vlak Voor\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("    If Not ok Then");
            sb.AppendLine("        MsgBox \"Kan Front Plane/Vlak Voor niet selecteren.\"");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine();
            sb.AppendLine("    swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    rectSegments = swModel.SketchManager.CreateCenterRectangle(0, 0, 0, x / 2, y / 2, 0)");
            sb.AppendLine("    FullyDefineSketchWithDimensions swModel");
            sb.AppendLine("    swModel.SketchManager.InsertSketch True");
            sb.AppendLine();
            sb.AppendLine("    swModel.FeatureManager.FeatureExtrusion2 True, False, False, 6, 0, z, 0, False, False, False, False, 0, 0, False, False, False, False, True, True, True, 0, 0, False");
            sb.AppendLine("    If Len(drillData) > 0 Then CreateProfileDrillFeatures swModel, drillData, lengthAxis, x, y, z");
            sb.AppendLine("    AddPartMetadata swModel, x, y, z");
            sb.AppendLine("    swModel.Extension.SaveAs filePath, 0, 1, Nothing, errors, warnings");
            sb.AppendLine("    swApp.CloseDoc swModel.GetTitle");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateProfileDrillFeatures(swModel As Object, drillData As String, lengthAxis As String, x As Double, y As Double, z As Double)");
            sb.AppendLine("    If lengthAxis = \"Y\" Then");
            sb.AppendLine("        CreateProfileDrillCut swModel, drillData, lengthAxis, x, y, z, \"X\"");
            sb.AppendLine("        CreateProfileDrillCut swModel, drillData, lengthAxis, x, y, z, \"Z\"");
            sb.AppendLine("    Else");
            sb.AppendLine("        CreateProfileDrillCut swModel, drillData, lengthAxis, x, y, z, \"\"");
            sb.AppendLine("    End If");
            sb.AppendLine("    'Kopse tapgaten blijven voorlopig in de boor/taplijst. Die vragen een face-specifieke boring vanaf het kopvlak.");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateProfileDrillCut(swModel As Object, drillData As String, lengthAxis As String, x As Double, y As Double, z As Double, sideCode As String)");
            sb.AppendLine("    Dim drills As Variant");
            sb.AppendLine("    Dim fields As Variant");
            sb.AppendLine("    Dim i As Long");
            sb.AppendLine("    Dim ok As Boolean");
            sb.AppendLine("    Dim hasHoles As Boolean");
            sb.AppendLine("    Dim cutDepth As Double");
            sb.AppendLine("    Dim pos As Double");
            sb.AppendLine("    Dim dia As Double");
            sb.AppendLine("    Dim isThrough As Boolean");
            sb.AppendLine("    Dim sideText As String");
            sb.AppendLine("    Dim cutFeat As Object");
            sb.AppendLine("    drills = Split(drillData, \"|\")");
            sb.AppendLine("    swModel.ClearSelection2 True");
            sb.AppendLine("    If lengthAxis = \"Z\" Or sideCode = \"X\" Then");
            sb.AppendLine("        ok = swModel.Extension.SelectByID2(\"Right Plane\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        If Not ok Then ok = swModel.Extension.SelectByID2(\"Vlak Rechts\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        cutDepth = x + 0.002");
            sb.AppendLine("    Else");
            sb.AppendLine("        ok = swModel.Extension.SelectByID2(\"Front Plane\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        If Not ok Then ok = swModel.Extension.SelectByID2(\"Vlak Voor\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        cutDepth = z + 0.002");
            sb.AppendLine("    End If");
            sb.AppendLine("    If Not ok Then Exit Sub");
            sb.AppendLine("    swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swModel.SketchManager.AddToDB = True");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    For i = LBound(drills) To UBound(drills)");
            sb.AppendLine("        fields = Split(drills(i), \";\")");
            sb.AppendLine("        If UBound(fields) >= 4 Then");
            sb.AppendLine("            isThrough = (fields(4) = \"1\")");
            sb.AppendLine("            sideText = fields(1)");
            sb.AppendLine("            If isThrough Then");
            sb.AppendLine("                If sideCode = \"X\" And InStr(1, sideText, \"X-zijde\", vbTextCompare) = 0 Then GoTo NextProfileDrill");
            sb.AppendLine("                If sideCode = \"Z\" And InStr(1, sideText, \"Z-zijde\", vbTextCompare) = 0 Then GoTo NextProfileDrill");
            sb.AppendLine("                pos = Val(fields(2))");
            sb.AppendLine("                dia = Val(fields(3))");
            sb.AppendLine("                If dia > 0 Then");
            sb.AppendLine("                    If lengthAxis = \"X\" Then");
            sb.AppendLine("                        swModel.SketchManager.CreateCircleByRadius -x / 2 + pos, 0, 0, dia / 2");
            sb.AppendLine("                    ElseIf lengthAxis = \"Y\" Then");
            sb.AppendLine("                        swModel.SketchManager.CreateCircleByRadius 0, -y / 2 + pos, 0, dia / 2");
            sb.AppendLine("                    Else");
            sb.AppendLine("                        swModel.SketchManager.CreateCircleByRadius 0, 0, -z / 2 + pos, dia / 2");
            sb.AppendLine("                    End If");
            sb.AppendLine("                    hasHoles = True");
            sb.AppendLine("                End If");
            sb.AppendLine("            End If");
            sb.AppendLine("        End If");
            sb.AppendLine("NextProfileDrill:");
            sb.AppendLine("    Next");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swModel.SketchManager.AddToDB = False");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    If Not hasHoles Then");
            sb.AppendLine("        swModel.SketchManager.InsertSketch True");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    Set cutFeat = swModel.FeatureManager.FeatureCut3(False, False, False, 0, 0, cutDepth / 2, cutDepth / 2, False, False, False, False, 0, 0, False, False, False, False, False, True, True, False, True, False, 0, 0, False)");
            sb.AppendLine("    If Err.Number <> 0 Or cutFeat Is Nothing Then");
            sb.AppendLine("        Err.Clear");
            sb.AppendLine("        Set cutFeat = swModel.FeatureManager.FeatureCut3(True, False, False, 0, 0, cutDepth, 0, False, False, False, False, 0, 0, False, False, False, False, False, True, True, False, True, False, 0, 0, False)");
            sb.AppendLine("        If Err.Number <> 0 Or cutFeat Is Nothing Then");
            sb.AppendLine("            Err.Clear");
            sb.AppendLine("            swModel.SketchManager.InsertSketch True");
            sb.AppendLine("        End If");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    swModel.EditRebuild3");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateSheetPart(filePath As String, x As Double, thickness As Double, z As Double, notch As Double, holeData As String, orientation As String, toeDepth As Double, toeHeight As Double)");
            sb.AppendLine("    Dim swModel As Object");
            sb.AppendLine("    Dim ok As Boolean");
            sb.AppendLine("    Dim errors As Long");
            sb.AppendLine("    Dim warnings As Long");
            sb.AppendLine("    Dim baseFeat As Object");
            sb.AppendLine("    Dim extrudeDepth As Double");
            sb.AppendLine("    Set swModel = swApp.NewPart");
            sb.AppendLine("    If swModel Is Nothing Then");
            sb.AppendLine("        MsgBox \"Kan geen nieuw sheet-part maken. Controleer de default part template in SOLIDWORKS.\"");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    ok = SelectSheetBasePlane(swModel, orientation)");
            sb.AppendLine("    If Not ok Then");
            sb.AppendLine("        MsgBox \"Kan sheet-plane niet selecteren.\"");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    If notch > 0 And orientation = \"HORIZONTAL\" Then");
            sb.AppendLine("        CreateNotchedSheetSketch swModel, x, z, notch");
            sb.AppendLine("    Else");
            sb.AppendLine("        CreateSheetRectangle swModel, x, thickness, z, orientation");
            sb.AppendLine("    End If");
            sb.AppendLine("    If notch = 0 Then FullyDefineSketchWithDimensions swModel");
            sb.AppendLine("    swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    extrudeDepth = thickness");
            sb.AppendLine("    If orientation = \"VERTICAL_Z\" Then extrudeDepth = x");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    If orientation = \"VERTICAL_X\" Or orientation = \"VERTICAL_Z\" Then");
            sb.AppendLine("        Set baseFeat = swModel.FeatureManager.FeatureExtrusion2(True, False, False, 6, 0, extrudeDepth, 0, False, False, False, False, 0, 0, False, False, False, False, True, True, True, 0, 0, False)");
            sb.AppendLine("    Else");
            sb.AppendLine("        Set baseFeat = swModel.FeatureManager.FeatureExtrusion2(True, False, True, 0, 0, extrudeDepth, 0, False, False, False, False, 0, 0, False, False, False, False, True, True, True, 0, 0, False)");
            sb.AppendLine("        If Err.Number <> 0 Or baseFeat Is Nothing Then");
            sb.AppendLine("            Err.Clear");
            sb.AppendLine("            Set baseFeat = swModel.FeatureManager.FeatureExtrusion2(True, False, False, 0, 0, extrudeDepth, 0, False, False, False, False, 0, 0, False, False, False, False, True, True, True, 0, 0, False)");
            sb.AppendLine("        End If");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    If baseFeat Is Nothing Then");
            sb.AppendLine("        MsgBox \"Kan sheet basis niet extruden:\" & vbCrLf & filePath & vbCrLf & \"Orientatie: \" & orientation");
            sb.AppendLine("        swApp.CloseDoc swModel.GetTitle");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    If toeDepth > 0 And toeHeight > 0 Then CreateToeKickCut swModel, x, z, thickness, toeDepth, toeHeight, orientation");
            sb.AppendLine("    If Len(holeData) > 0 Then CreateCountersinkFeatures swModel, holeData, x, z, thickness, orientation");
            sb.AppendLine("    If Len(holeData) > 0 Then CreateThroughHoleFeatures swModel, holeData, thickness, orientation");
            sb.AppendLine("    AddPartMetadata swModel, x, thickness, z");
            sb.AppendLine("    swModel.Extension.SaveAs filePath, 0, 1, Nothing, errors, warnings");
            sb.AppendLine("    swApp.CloseDoc swModel.GetTitle");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateToeKickCut(swModel As Object, depth As Double, height As Double, thickness As Double, toeDepth As Double, toeHeight As Double, orientation As String)");
            sb.AppendLine("    Dim ok As Boolean");
            sb.AppendLine("    Dim cutFeat As Object");
            sb.AppendLine("    Dim y0 As Double");
            sb.AppendLine("    Dim y1 As Double");
            sb.AppendLine("    Dim z0 As Double");
            sb.AppendLine("    Dim z1 As Double");
            sb.AppendLine("    If orientation <> \"VERTICAL_Z\" Then Exit Sub");
            sb.AppendLine("    y0 = -height / 2");
            sb.AppendLine("    y1 = y0 + toeHeight");
            sb.AppendLine("    z0 = -depth / 2");
            sb.AppendLine("    z1 = z0 + toeDepth");
            sb.AppendLine("    swModel.ClearSelection2 True");
            sb.AppendLine("    ok = SelectSheetPlane(swModel, orientation)");
            sb.AppendLine("    If Not ok Then Exit Sub");
            sb.AppendLine("    swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, y0, z0, 0, y1, z0");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, y1, z0, 0, y1, z1");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, y1, z1, 0, y0, z1");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, y0, z1, 0, y0, z0");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    Set cutFeat = swModel.FeatureManager.FeatureCut3(False, False, False, 0, 0, thickness / 2 + 0.002, thickness / 2 + 0.002, False, False, False, False, 0, 0, False, False, False, False, False, True, True, False, True, False, 0, 0, False)");
            sb.AppendLine("    If Err.Number <> 0 Or cutFeat Is Nothing Then");
            sb.AppendLine("        Err.Clear");
            sb.AppendLine("        swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    swModel.EditRebuild3");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Function SelectSheetBasePlane(swModel As Object, orientation As String) As Boolean");
            sb.AppendLine("    Dim ok As Boolean");
            sb.AppendLine("    If orientation = \"VERTICAL_X\" Or orientation = \"VERTICAL_Z\" Then");
            sb.AppendLine("        ok = swModel.Extension.SelectByID2(\"Front Plane\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        If Not ok Then ok = swModel.Extension.SelectByID2(\"Vlak Voor\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("    Else");
            sb.AppendLine("        ok = swModel.Extension.SelectByID2(\"Top Plane\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        If Not ok Then ok = swModel.Extension.SelectByID2(\"Vlak Boven\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("    End If");
            sb.AppendLine("    SelectSheetBasePlane = ok");
            sb.AppendLine("End Function");
            sb.AppendLine();
            sb.AppendLine("Function SelectSheetPlane(swModel As Object, orientation As String) As Boolean");
            sb.AppendLine("    Dim ok As Boolean");
            sb.AppendLine("    If orientation = \"VERTICAL_X\" Then");
            sb.AppendLine("        ok = swModel.Extension.SelectByID2(\"Front Plane\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        If Not ok Then ok = swModel.Extension.SelectByID2(\"Vlak Voor\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("    ElseIf orientation = \"VERTICAL_Z\" Then");
            sb.AppendLine("        ok = swModel.Extension.SelectByID2(\"Right Plane\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        If Not ok Then ok = swModel.Extension.SelectByID2(\"Vlak Rechts\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("    Else");
            sb.AppendLine("        ok = swModel.Extension.SelectByID2(\"Top Plane\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("        If Not ok Then ok = swModel.Extension.SelectByID2(\"Vlak Boven\", \"PLANE\", 0, 0, 0, False, 0, Nothing, 0)");
            sb.AppendLine("    End If");
            sb.AppendLine("    SelectSheetPlane = ok");
            sb.AppendLine("End Function");
            sb.AppendLine();
            sb.AppendLine("Sub CreateSheetRectangle(swModel As Object, x As Double, thickness As Double, z As Double, orientation As String)");
            sb.AppendLine("    If orientation = \"VERTICAL_Z\" Then");
            sb.AppendLine("        swModel.SketchManager.CreateCenterRectangle 0, 0, 0, thickness / 2, z / 2, 0");
            sb.AppendLine("    Else");
            sb.AppendLine("        swModel.SketchManager.CreateCenterRectangle 0, 0, 0, x / 2, z / 2, 0");
            sb.AppendLine("    End If");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateSheetHoleCircle(swModel As Object, hx As Double, hy As Double, radius As Double, orientation As String)");
            sb.AppendLine("    If orientation = \"VERTICAL_Z\" Then");
            sb.AppendLine("        swModel.SketchManager.CreateCircleByRadius 0, hy, hx, radius");
            sb.AppendLine("    Else");
            sb.AppendLine("        swModel.SketchManager.CreateCircleByRadius hx, hy, 0, radius");
            sb.AppendLine("    End If");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateThroughHoleFeatures(swModel As Object, holeData As String, thickness As Double, orientation As String)");
            sb.AppendLine("    Dim holes As Variant");
            sb.AppendLine("    Dim fields As Variant");
            sb.AppendLine("    Dim i As Long");
            sb.AppendLine("    Dim ok As Boolean");
            sb.AppendLine("    Dim hasHoles As Boolean");
            sb.AppendLine("    Dim cutFeat As Object");
            sb.AppendLine("    holes = Split(holeData, \"|\")");
            sb.AppendLine("    swModel.ClearSelection2 True");
            sb.AppendLine("    ok = SelectSheetPlane(swModel, orientation)");
            sb.AppendLine("    If Not ok Then Exit Sub");
            sb.AppendLine("    swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swModel.SketchManager.AddToDB = True");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    For i = LBound(holes) To UBound(holes)");
            sb.AppendLine("        fields = Split(holes(i), \";\")");
            sb.AppendLine("        If UBound(fields) >= 2 Then");
            sb.AppendLine("            CreateSheetHoleCircle swModel, Val(fields(0)), Val(fields(1)), Val(fields(2)) / 2, orientation");
            sb.AppendLine("            hasHoles = True");
            sb.AppendLine("        End If");
            sb.AppendLine("    Next");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swModel.SketchManager.AddToDB = False");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    If Not hasHoles Then");
            sb.AppendLine("        swModel.SketchManager.InsertSketch True");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    If orientation = \"VERTICAL_X\" Or orientation = \"VERTICAL_Z\" Then");
            sb.AppendLine("        Set cutFeat = swModel.FeatureManager.FeatureCut3(False, False, False, 0, 0, thickness / 2 + 0.002, thickness / 2 + 0.002, False, False, False, False, 0, 0, False, False, False, False, False, True, True, False, True, False, 0, 0, False)");
            sb.AppendLine("    Else");
            sb.AppendLine("        Set cutFeat = swModel.FeatureManager.FeatureCut3(True, False, False, 0, 0, thickness + 0.002, 0, False, False, False, False, 0, 0, False, False, False, False, False, True, True, False, True, False, 0, 0, False)");
            sb.AppendLine("    End If");
            sb.AppendLine("    If Err.Number <> 0 Or cutFeat Is Nothing Then");
            sb.AppendLine("        Err.Clear");
            sb.AppendLine("        swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    swModel.EditRebuild3");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateCountersinkFeatures(swModel As Object, holeData As String, sheetX As Double, sheetZ As Double, thickness As Double, orientation As String)");
            sb.AppendLine("    Dim holes As Variant");
            sb.AppendLine("    Dim fields As Variant");
            sb.AppendLine("    Dim i As Long");
            sb.AppendLine("    Dim ok As Boolean");
            sb.AppendLine("    Dim countersinkDiameter As Double");
            sb.AppendLine("    Dim countersinkDepth As Double");
            sb.AppendLine("    Dim maxDepth As Double");
            sb.AppendLine("    Dim hasCountersinks As Boolean");
            sb.AppendLine("    Dim cutFeat As Object");
            sb.AppendLine("    holes = Split(holeData, \"|\")");
            sb.AppendLine("    swModel.ClearSelection2 True");
            sb.AppendLine("    ok = SelectSheetPlane(swModel, orientation)");
            sb.AppendLine("    If Not ok Then Exit Sub");
            sb.AppendLine("    swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swModel.SketchManager.AddToDB = True");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    For i = LBound(holes) To UBound(holes)");
            sb.AppendLine("        fields = Split(holes(i), \";\")");
            sb.AppendLine("        If UBound(fields) >= 4 Then");
            sb.AppendLine("            countersinkDiameter = Val(fields(3))");
            sb.AppendLine("            countersinkDepth = Val(fields(4))");
            sb.AppendLine("            If countersinkDiameter > 0 And countersinkDepth > 0 Then");
            sb.AppendLine("                CreateSheetHoleCircle swModel, Val(fields(0)), Val(fields(1)), countersinkDiameter / 2, orientation");
            sb.AppendLine("                If countersinkDepth > maxDepth Then maxDepth = countersinkDepth");
            sb.AppendLine("                hasCountersinks = True");
            sb.AppendLine("            End If");
            sb.AppendLine("        End If");
            sb.AppendLine("    Next");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swModel.SketchManager.AddToDB = False");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    If Not hasCountersinks Then");
            sb.AppendLine("        swModel.SketchManager.InsertSketch True");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    Set cutFeat = swModel.FeatureManager.FeatureCut3(True, False, False, 0, 0, maxDepth, 0, False, False, False, False, 0, 0, False, False, False, False, False, True, True, False, True, False, 0, 0, False)");
            sb.AppendLine("    If Err.Number <> 0 Or cutFeat Is Nothing Then");
            sb.AppendLine("        Err.Clear");
            sb.AppendLine("        swModel.SketchManager.InsertSketch True");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("    swModel.EditRebuild3");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateNotchedSheetSketch(swModel As Object, x As Double, z As Double, notch As Double)");
            sb.AppendLine("    Dim hx As Double");
            sb.AppendLine("    Dim hz As Double");
            sb.AppendLine("    hx = x / 2");
            sb.AppendLine("    hz = z / 2");
            sb.AppendLine("    swModel.SketchManager.CreateLine -hx + notch, -hz, 0, hx - notch, -hz, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine hx - notch, -hz, 0, hx - notch, -hz + notch, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine hx - notch, -hz + notch, 0, hx, -hz + notch, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine hx, -hz + notch, 0, hx, hz - notch, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine hx, hz - notch, 0, hx - notch, hz - notch, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine hx - notch, hz - notch, 0, hx - notch, hz, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine hx - notch, hz, 0, -hx + notch, hz, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine -hx + notch, hz, 0, -hx + notch, hz - notch, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine -hx + notch, hz - notch, 0, -hx, hz - notch, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine -hx, hz - notch, 0, -hx, -hz + notch, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine -hx, -hz + notch, 0, -hx + notch, -hz + notch, 0");
            sb.AppendLine("    swModel.SketchManager.CreateLine -hx + notch, -hz + notch, 0, -hx + notch, -hz, 0");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateVerticalZToeKickSheetSketch(swModel As Object, depth As Double, height As Double, toeDepth As Double, toeHeight As Double)");
            sb.AppendLine("    Dim yBottom As Double");
            sb.AppendLine("    Dim yTop As Double");
            sb.AppendLine("    Dim zFront As Double");
            sb.AppendLine("    Dim zBack As Double");
            sb.AppendLine("    Dim notchY As Double");
            sb.AppendLine("    Dim notchZ As Double");
            sb.AppendLine("    yBottom = -height / 2");
            sb.AppendLine("    yTop = height / 2");
            sb.AppendLine("    zFront = -depth / 2");
            sb.AppendLine("    zBack = depth / 2");
            sb.AppendLine("    notchY = yBottom + toeHeight");
            sb.AppendLine("    notchZ = zFront + toeDepth");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, yBottom, notchZ, 0, yBottom, zBack");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, yBottom, zBack, 0, yTop, zBack");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, yTop, zBack, 0, yTop, zFront");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, yTop, zFront, 0, notchY, zFront");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, notchY, zFront, 0, notchY, notchZ");
            sb.AppendLine("    swModel.SketchManager.CreateLine 0, notchY, notchZ, 0, yBottom, notchZ");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub FullyDefineSketchWithDimensions(swModel As Object)");
            sb.AppendLine("    Dim status As Long");
            sb.AppendLine("    Dim datumOk As Boolean");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swModel.ClearSelection2 True");
            sb.AppendLine("    datumOk = swModel.Extension.SelectByID2(\"Point1@Origin\", \"EXTSKETCHPOINT\", 0, 0, 0, False, 6, Nothing, 0)");
            sb.AppendLine("    status = swModel.SketchManager.FullyDefineSketch(True, True, 3, True, 1, Nothing, 1, Nothing, 1, 1)");
            sb.AppendLine("    swModel.ClearSelection2 True");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub AddPartMetadata(swModel As Object, x As Double, y As Double, z As Double)");
            sb.AppendLine("    Dim props As Object");
            sb.AppendLine("    Set props = swModel.Extension.CustomPropertyManager(\"\")");
            sb.AppendLine("    props.Add3 \"Maat_X_mm\", 30, CStr(Round(x * 1000, 3)), 2");
            sb.AppendLine("    props.Add3 \"Maat_Y_mm\", 30, CStr(Round(y * 1000, 3)), 2");
            sb.AppendLine("    props.Add3 \"Maat_Z_mm\", 30, CStr(Round(z * 1000, 3)), 2");
            sb.AppendLine("    props.Add3 \"Generator\", 30, \"SW Werkplaats Configurator\", 2");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub CreateAssembly(filePath As String)");
            sb.AppendLine("    Dim swAsm As Object");
            sb.AppendLine("    Dim errors As Long");
            sb.AppendLine("    Dim warnings As Long");
            sb.AppendLine("    Set swAsm = swApp.NewAssembly");
            sb.AppendLine("    If swAsm Is Nothing Then");
            sb.AppendLine("        MsgBox \"Kan geen nieuwe assembly maken. Controleer de default assembly template in SOLIDWORKS.\"");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    swAsm.Extension.SaveAs filePath, 0, 1, Nothing, errors, warnings");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub AddPart(partPath As String, x As Double, y As Double, z As Double)");
            sb.AppendLine("    AddPartOriented partPath, x, y, z, \"\"");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub AddPartOriented(partPath As String, x As Double, y As Double, z As Double, orientation As String)");
            sb.AppendLine("    Dim swModel As Object");
            sb.AppendLine("    Dim swAsm As Object");
            sb.AppendLine("    Dim swPart As Object");
            sb.AppendLine("    Dim swComp As Object");
            sb.AppendLine("    Dim loadErrors As Long");
            sb.AppendLine("    Dim loadWarnings As Long");
            sb.AppendLine("    Dim activateErrors As Long");
            sb.AppendLine("    Dim asmTitle As String");
            sb.AppendLine("    Dim partTitle As String");
            sb.AppendLine("    Dim errText As String");
            sb.AppendLine("    Set swModel = swApp.ActiveDoc");
            sb.AppendLine("    asmTitle = swModel.GetTitle");
            sb.AppendLine("    Set swAsm = swModel");
            sb.AppendLine();
            sb.AppendLine("    If Dir(partPath) = \"\" Then");
            sb.AppendLine("        MsgBox \"Partbestand bestaat niet:\" & vbCrLf & partPath");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine();
            sb.AppendLine("    Set swPart = swApp.OpenDoc6(partPath, 1, 1, \"\", loadErrors, loadWarnings)");
            sb.AppendLine("    If swPart Is Nothing Then");
            sb.AppendLine("        MsgBox \"Kan part niet laden voor assembly:\" & vbCrLf & partPath & vbCrLf & \"OpenDoc6 errors: \" & CStr(loadErrors)");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    partTitle = swPart.GetTitle");
            sb.AppendLine("    Set swModel = swApp.ActivateDoc3(asmTitle, False, 0, activateErrors)");
            sb.AppendLine("    If swModel Is Nothing Then");
            sb.AppendLine("        MsgBox \"Kan assembly niet opnieuw activeren:\" & vbCrLf & asmTitle & vbCrLf & \"ActivateDoc3 errors: \" & CStr(activateErrors)");
            sb.AppendLine("        If Len(partTitle) > 0 Then swApp.CloseDoc partTitle");
            sb.AppendLine("        Exit Sub");
            sb.AppendLine("    End If");
            sb.AppendLine("    Set swAsm = swModel");
            sb.AppendLine();
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    Set swComp = swAsm.AddComponent5(partPath, 0, \"\", False, \"\", x, y, z)");
            sb.AppendLine("    If Err.Number <> 0 Then errText = \"AddComponent5: \" & Err.Description");
            sb.AppendLine("    If swComp Is Nothing Then");
            sb.AppendLine("        Err.Clear");
            sb.AppendLine("        Set swComp = swAsm.AddComponent4(partPath, \"\", x, y, z)");
            sb.AppendLine("        If Err.Number <> 0 Then errText = errText & vbCrLf & \"AddComponent4: \" & Err.Description");
            sb.AppendLine("    End If");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine();
            sb.AppendLine("    If swComp Is Nothing Then");
            sb.AppendLine("        MsgBox \"Kon component niet toevoegen:\" & vbCrLf & partPath & vbCrLf & errText");
            sb.AppendLine("    Else");
            sb.AppendLine("        If orientation = \"SHEET_XY_TO_XZ\" Then SetSheetComponentTransform swComp, x, y, z");
            sb.AppendLine("        If orientation = \"SHEET_VERTICAL_X\" Then SetComponentTransform swComp, x, y, z, 1#, 0#, 0#, 0#, 0#, 1#, 0#, 1#, 0#");
            sb.AppendLine("        If orientation = \"SHEET_VERTICAL_Z\" Then SetComponentTransform swComp, x, y, z, 0#, 0#, 1#, 1#, 0#, 0#, 0#, 1#, 0#");
            sb.AppendLine("        On Error Resume Next");
            sb.AppendLine("        swComp.Select4 False, Nothing, False");
            sb.AppendLine("        swAsm.FixComponent");
            sb.AppendLine("        On Error GoTo 0");
            sb.AppendLine("    End If");
            sb.AppendLine();
            sb.AppendLine("    If Len(partTitle) > 0 Then swApp.CloseDoc partTitle");
            sb.AppendLine("    Set swModel = swApp.ActivateDoc3(asmTitle, False, 0, activateErrors)");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub SetComponentTransform(swComp As Object, x As Double, y As Double, z As Double, ax As Double, ay As Double, az As Double, bx As Double, by As Double, bz As Double, cx As Double, cy As Double, cz As Double)");
            sb.AppendLine("    Dim swMathUtil As Object");
            sb.AppendLine("    Dim swTransform As Object");
            sb.AppendLine("    Dim data(15) As Double");
            sb.AppendLine("    Set swMathUtil = swApp.GetMathUtility");
            sb.AppendLine("    data(0) = ax: data(1) = ay: data(2) = az");
            sb.AppendLine("    data(3) = bx: data(4) = by: data(5) = bz");
            sb.AppendLine("    data(6) = cx: data(7) = cy: data(8) = cz");
            sb.AppendLine("    data(9) = x: data(10) = y: data(11) = z");
            sb.AppendLine("    data(12) = 1#: data(13) = 0#: data(14) = 0#: data(15) = 0#");
            sb.AppendLine("    Set swTransform = swMathUtil.CreateTransform(data)");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swComp.Transform2 = swTransform");
            sb.AppendLine("    swComp.SetTransformAndSolve2 swTransform");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("End Sub");
            sb.AppendLine();
            sb.AppendLine("Sub SetSheetComponentTransform(swComp As Object, x As Double, y As Double, z As Double)");
            sb.AppendLine("    Dim swMathUtil As Object");
            sb.AppendLine("    Dim swTransform As Object");
            sb.AppendLine("    Dim data(15) As Double");
            sb.AppendLine("    Set swMathUtil = swApp.GetMathUtility");
            sb.AppendLine("    data(0) = 1#: data(1) = 0#: data(2) = 0#");
            sb.AppendLine("    data(3) = 0#: data(4) = 0#: data(5) = -1#");
            sb.AppendLine("    data(6) = 0#: data(7) = 1#: data(8) = 0#");
            sb.AppendLine("    data(9) = x: data(10) = y: data(11) = z");
            sb.AppendLine("    data(12) = 1#: data(13) = 0#: data(14) = 0#: data(15) = 0#");
            sb.AppendLine("    Set swTransform = swMathUtil.CreateTransform(data)");
            sb.AppendLine("    On Error Resume Next");
            sb.AppendLine("    swComp.Transform2 = swTransform");
            sb.AppendLine("    swComp.SetTransformAndSolve2 swTransform");
            sb.AppendLine("    On Error GoTo 0");
            sb.AppendLine("End Sub");

            return sb.ToString();
        }

        private static void AppendAssemblyCalls(StringBuilder sb, WorkbenchModel model, string cadFolder)
        {
            if (model.AssemblyPlacements.Count > 0)
            {
                AppendGenericAssemblyCalls(sb, model, cadFolder);
                return;
            }

            if (model.Sheets.Count == 0 || model.Profiles.Count == 0)
            {
                return;
            }

            var top = model.Sheets[0];
            var leg = FindProfile(model, "Poot");
            if (leg == null)
            {
                return;
            }

            var width = top.LengthMm;
            var depth = top.WidthMm;
            var profile = leg.Material.WidthMm;
            var legLength = leg.LengthMm;
            var topFrameY = legLength - profile / 2.0;
            var lowerY = model.LowerFrameHeightMm;
            var middleY = model.MiddleLayerHeightMm;

            sb.AppendLine("    CreateAssembly assemblyPath");

            var legPath = Path.Combine(cadFolder, SafeName(leg.Name) + "_" + leg.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
            AddComponent(sb, legPath, -width / 2 + profile / 2, legLength / 2, -depth / 2 + profile / 2);
            AddComponent(sb, legPath, width / 2 - profile / 2, legLength / 2, -depth / 2 + profile / 2);
            AddComponent(sb, legPath, -width / 2 + profile / 2, legLength / 2, depth / 2 - profile / 2);
            AddComponent(sb, legPath, width / 2 - profile / 2, legLength / 2, depth / 2 - profile / 2);

            var frontBack = FindProfile(model, "Bovenframe voor/achter");
            if (frontBack != null)
            {
                var path = Path.Combine(cadFolder, SafeName(frontBack.Name) + "_" + frontBack.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
                AddComponent(sb, path, 0, topFrameY, -depth / 2 + profile / 2);
                AddComponent(sb, path, 0, topFrameY, depth / 2 - profile / 2);
            }

            var leftRight = FindProfile(model, "Bovenframe links/rechts");
            if (leftRight != null)
            {
                var path = Path.Combine(cadFolder, SafeName(leftRight.Name) + "_" + leftRight.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
                AddComponent(sb, path, -width / 2 + profile / 2, topFrameY, 0);
                AddComponent(sb, path, width / 2 - profile / 2, topFrameY, 0);
            }

            var lowerFrontBack = FindProfile(model, "Onderframe voor/achter");
            if (lowerFrontBack != null)
            {
                var path = Path.Combine(cadFolder, SafeName(lowerFrontBack.Name) + "_" + lowerFrontBack.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
                AddComponent(sb, path, 0, lowerY, -depth / 2 + profile / 2);
                AddComponent(sb, path, 0, lowerY, depth / 2 - profile / 2);
            }

            var lowerLeftRight = FindProfile(model, "Onderframe links/rechts");
            if (lowerLeftRight != null)
            {
                var path = Path.Combine(cadFolder, SafeName(lowerLeftRight.Name) + "_" + lowerLeftRight.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
                AddComponent(sb, path, -width / 2 + profile / 2, lowerY, 0);
                AddComponent(sb, path, width / 2 - profile / 2, lowerY, 0);
            }

            var middleFrontBack = FindProfile(model, "Tussenframe voor/achter");
            if (middleFrontBack != null)
            {
                var path = Path.Combine(cadFolder, SafeName(middleFrontBack.Name) + "_" + middleFrontBack.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
                AddComponent(sb, path, 0, middleY, -depth / 2 + profile / 2);
                AddComponent(sb, path, 0, middleY, depth / 2 - profile / 2);
            }

            var middleLeftRight = FindProfile(model, "Tussenframe links/rechts");
            if (middleLeftRight != null)
            {
                var path = Path.Combine(cadFolder, SafeName(middleLeftRight.Name) + "_" + middleLeftRight.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
                AddComponent(sb, path, -width / 2 + profile / 2, middleY, 0);
                AddComponent(sb, path, width / 2 - profile / 2, middleY, 0);
            }

            foreach (var sheet in model.Sheets)
            {
                var path = Path.Combine(cadFolder, SafeName(sheet.Name) + "_" + sheet.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "x" + sheet.WidthMm.ToString("0", CultureInfo.InvariantCulture) + ".SLDPRT");
                AddSheetComponent(sb, path, 0, sheet.CenterHeightMm, 0);
            }

            sb.AppendLine("    Dim asmErrors As Long");
            sb.AppendLine("    Dim asmWarnings As Long");
            sb.AppendLine("    swApp.ActiveDoc.EditRebuild3");
            sb.AppendLine("    swApp.ActiveDoc.ViewZoomtofit2");
            sb.AppendLine("    swApp.ActiveDoc.Extension.SaveAs assemblyPath, 0, 1, Nothing, asmErrors, asmWarnings");
        }

        private static void AppendGenericAssemblyCalls(StringBuilder sb, WorkbenchModel model, string cadFolder)
        {
            sb.AppendLine("    CreateAssembly assemblyPath");
            foreach (var placement in model.AssemblyPlacements)
            {
                string path;
                if (placement.Kind == AssemblyComponentKind.Profile)
                {
                    path = Path.Combine(cadFolder, SafeName(placement.PartName) + "_" + placement.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "mm.SLDPRT");
                }
                else
                {
                    path = Path.Combine(cadFolder, SafeName(placement.PartName) + "_" + placement.LengthMm.ToString("0", CultureInfo.InvariantCulture) + "x" + placement.WidthMm.ToString("0", CultureInfo.InvariantCulture) + ".SLDPRT");
                }

                AddComponentOriented(sb, path, placement.Xmm, placement.Ymm, placement.Zmm, "");
            }

            sb.AppendLine("    Dim asmErrors As Long");
            sb.AppendLine("    Dim asmWarnings As Long");
            sb.AppendLine("    swApp.ActiveDoc.EditRebuild3");
            sb.AppendLine("    swApp.ActiveDoc.ViewZoomtofit2");
            sb.AppendLine("    swApp.ActiveDoc.Extension.SaveAs assemblyPath, 0, 1, Nothing, asmErrors, asmWarnings");
        }

        private static void AddComponent(StringBuilder sb, string path, double xMm, double yMm, double zMm)
        {
            sb.AppendLine("    AddPart " + Q(path) + ", " + M(xMm) + ", " + M(yMm) + ", " + M(zMm));
        }

        private static void AddSheetComponent(StringBuilder sb, string path, double xMm, double yMm, double zMm)
        {
            sb.AppendLine("    AddPart " + Q(path) + ", " + M(xMm) + ", " + M(yMm) + ", " + M(zMm));
        }

        private static void AddComponentOriented(StringBuilder sb, string path, double xMm, double yMm, double zMm, string orientation)
        {
            sb.AppendLine("    AddPartOriented " + Q(path) + ", " + M(xMm) + ", " + M(yMm) + ", " + M(zMm) + ", " + Q(orientation));
        }

        private static string OrientationCode(AssemblyOrientation orientation)
        {
            if (orientation == AssemblyOrientation.SheetVerticalX) return "SHEET_VERTICAL_X";
            if (orientation == AssemblyOrientation.SheetVerticalZ) return "SHEET_VERTICAL_Z";
            return "";
        }

        private static string SheetPartOrientation(WorkbenchModel model, SheetPart sheet)
        {
            foreach (var placement in model.AssemblyPlacements)
            {
                if (placement.Kind == AssemblyComponentKind.Sheet && placement.PartName == sheet.Name)
                {
                    if (placement.Orientation == AssemblyOrientation.SheetVerticalX) return "VERTICAL_X";
                    if (placement.Orientation == AssemblyOrientation.SheetVerticalZ) return "VERTICAL_Z";
                    return "HORIZONTAL";
                }
            }

            return "HORIZONTAL";
        }

        private static ProfilePart FindProfile(WorkbenchModel model, string name)
        {
            foreach (var profile in model.Profiles)
            {
                if (profile.Name == name) return profile;
            }

            return null;
        }

        private static double ProfileX(ProfilePart profile)
        {
            if (profile.Name.Contains("voor/achter")) return profile.LengthMm;
            return profile.Material.WidthMm;
        }

        private static double ProfileY(ProfilePart profile)
        {
            if (profile.Name == "Poot") return profile.LengthMm;
            return profile.Material.HeightMm;
        }

        private static double ProfileZ(ProfilePart profile)
        {
            if (profile.Name.Contains("links/rechts")) return profile.LengthMm;
            return profile.Material.HeightMm;
        }

        private static string ProfileAxis(ProfilePart profile)
        {
            if (profile.Name == "Poot") return "Y";
            if (profile.Name.Contains("voor/achter")) return "X";
            if (profile.Name.Contains("links/rechts")) return "Z";
            return "X";
        }

        private static string ProfileDrillData(ProfilePart profile)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < profile.Drills.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var drill = profile.Drills[i];
                sb.Append(ProfileAxis(profile));
                sb.Append(';');
                sb.Append(DataText(drill.Side));
                sb.Append(';');
                sb.Append(M(drill.PositionFromEndAMm));
                sb.Append(';');
                sb.Append(M(drill.DiameterMm));
                sb.Append(';');
                sb.Append(drill.ThroughHole ? "1" : "0");
                sb.Append(';');
                sb.Append(DataText(drill.Note));
            }

            return sb.ToString();
        }

        private static string DataText(string value)
        {
            return (value ?? string.Empty).Replace("|", "/").Replace(";", "/").Replace("\"", "'");
        }

        private static string Q(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string M(double mm)
        {
            return (mm / 1000.0).ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static string HoleData(SheetPart sheet)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < sheet.Holes.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var hole = sheet.Holes[i];
                sb.Append(M(hole.Xmm - sheet.LengthMm / 2.0));
                sb.Append(';');
                sb.Append(M(hole.Ymm - sheet.WidthMm / 2.0));
                sb.Append(';');
                sb.Append(M(hole.DiameterMm));
                sb.Append(';');
                sb.Append(hole.Countersunk ? M(hole.CountersinkDiameterMm) : "0");
                sb.Append(';');
                sb.Append(hole.Countersunk ? M(hole.CountersinkDepthMm) : "0");
            }

            return sb.ToString();
        }

        private static string SafeName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace("/", "-");
        }
    }
}
