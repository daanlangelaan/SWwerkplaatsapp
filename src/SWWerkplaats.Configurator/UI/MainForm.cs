using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Engine;
using SWWerkplaats.Configurator.Manufacturing;
using SWWerkplaats.Configurator.SolidWorks;

namespace SWWerkplaats.Configurator.UI
{
    public sealed class MainForm : Form
    {
        private readonly NumericUpDown width;
        private readonly NumericUpDown depth;
        private readonly NumericUpDown height;
        private readonly NumericUpDown topThickness;
        private readonly NumericUpDown lowerFrameHeight;
        private readonly NumericUpDown middleLayerHeight;
        private readonly NumericUpDown shelfCornerClearance;
        private readonly NumericUpDown boltMaxSpacing;
        private readonly NumericUpDown countersinkDiameter;
        private readonly NumericUpDown countersinkDepth;
        private readonly NumericUpDown toolDiameter;
        private readonly NumericUpDown passDepth;
        private readonly NumericUpDown nestStockLength;
        private readonly NumericUpDown nestStockWidth;
        private readonly NumericUpDown nestSpacing;
        private readonly NumericUpDown nestMargin;
        private readonly NumericUpDown cabinetWidth;
        private readonly NumericUpDown cabinetDepth;
        private readonly NumericUpDown cabinetWorktopHeight;
        private readonly NumericUpDown cabinetUnitCount;
        private readonly NumericUpDown cabinetPlinthHeight;
        private readonly NumericUpDown cabinetPlinthDepth;
        private readonly NumericUpDown cabinetShelfClearance;
        private readonly NumericUpDown cabinetDrawerClearance;
        private readonly NumericUpDown cabinetDoorGap;
        private readonly ComboBox productMode;
        private readonly ComboBox frameProfile;
        private readonly ComboBox topSheet;
        private readonly ComboBox shelfSheet;
        private readonly ComboBox fastener;
        private readonly ComboBox cabinetCarcassMaterial;
        private readonly ComboBox cabinetWorktopMaterial;
        private readonly ComboBox cabinetDrawerMaterial;
        private readonly ComboBox cabinetFrontMaterial;
        private readonly ComboBox cabinetBackMaterial;
        private readonly ComboBox cabinetRailTemplate;
        private readonly CheckBox lowerFrame;
        private readonly CheckBox lowerShelf;
        private readonly CheckBox middleLayer;
        private readonly CheckBox middleShelf;
        private readonly CheckBox autoTabs;
        private readonly CheckBox countersinkHoles;
        private readonly CheckBox cabinetBackPanel;
        private readonly CheckBox exportSolidWorks;
        private readonly DataGridView cabinetUnits;
        private readonly TextBox outputFolder;
        private readonly TextBox log;

        public MainForm()
        {
            Text = "SW Werkplaats Configurator - Werktafel prototype";
            Width = 920;
            Height = 640;
            StartPosition = FormStartPosition.CenterScreen;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            var projectPage = BuildProjectPage();
            var cabinetPage = BuildCabinetPage();
            var camPage = BuildCamPage();
            var outputPage = BuildOutputPage();
            tabs.TabPages.Add(projectPage);
            tabs.TabPages.Add(cabinetPage);
            tabs.TabPages.Add(camPage);
            tabs.TabPages.Add(outputPage);
            Controls.Add(tabs);

            productMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
            productMode.Items.AddRange(new object[] { "Werktafel", "Cabinet" });
            productMode.SelectedIndex = 0;
            width = Number(1500, 300, 3020);
            depth = Number(750, 300, 1520);
            height = Number(900, 300, 2400);
            topThickness = Number(18, 6, 60);
            lowerFrameHeight = Number(180, 40, 2200);
            middleLayerHeight = Number(450, 80, 2200);
            shelfCornerClearance = Number(2, 0, 20);
            boltMaxSpacing = Number(300, 80, 1000);
            countersinkDiameter = Number(14, 8, 40);
            countersinkDepth = Number(8, 0.5m, 20);
            toolDiameter = Number(6, 1, 30);
            passDepth = Number(4, 0.5m, 20);
            nestStockLength = Number(3020, 300, 3020);
            nestStockWidth = Number(1520, 300, 1520);
            nestSpacing = Number(15, 0, 100);
            nestMargin = Number(15, 0, 100);
            cabinetWidth = Number(2400, 300, 3020);
            cabinetDepth = Number(600, 250, 1520);
            cabinetWorktopHeight = Number(900, 300, 2400);
            cabinetUnitCount = Number(4, 1, 12);
            cabinetPlinthHeight = Number(100, 0, 300);
            cabinetPlinthDepth = Number(60, 0, 300);
            cabinetShelfClearance = Number(2, 0, 20);
            cabinetDrawerClearance = Number(13, 0, 40);
            cabinetDoorGap = Number(2, 0, 10);
            frameProfile = MaterialCombo(LibraryCatalog.Profiles());
            topSheet = MaterialCombo(LibraryCatalog.Sheets(), 2);
            shelfSheet = MaterialCombo(LibraryCatalog.Sheets(), 2);
            fastener = FastenerCombo(LibraryCatalog.SheetFasteners(), 0);
            cabinetCarcassMaterial = MaterialCombo(LibraryCatalog.Sheets(), 2);
            cabinetWorktopMaterial = MaterialCombo(LibraryCatalog.Sheets(), 2);
            cabinetDrawerMaterial = MaterialCombo(LibraryCatalog.Sheets(), 3);
            cabinetFrontMaterial = MaterialCombo(LibraryCatalog.Sheets(), 2);
            cabinetBackMaterial = MaterialCombo(LibraryCatalog.Sheets(), 3);
            cabinetRailTemplate = RailCombo(LibraryCatalog.DrawerRails(), 1);
            lowerFrame = new CheckBox { Text = "Onderframe", Checked = false, AutoSize = true };
            lowerShelf = new CheckBox { Text = "Blad op onderframe met hoekuitsparingen", Checked = false, AutoSize = true };
            middleLayer = new CheckBox { Text = "Extra frame-laag", Checked = false, AutoSize = true };
            middleShelf = new CheckBox { Text = "Blad op extra laag met hoekuitsparingen", Checked = false, AutoSize = true };
            countersinkHoles = new CheckBox { Text = "Kopkamers frezen voor cilinderkop/inbusbout", Checked = true, AutoSize = true };
            cabinetBackPanel = new CheckBox { Text = "Achterwand toevoegen", Checked = false, AutoSize = true };
            autoTabs = new CheckBox { Text = "Tabs automatisch voor kleine delen", Checked = true, AutoSize = true };
            exportSolidWorks = new CheckBox { Text = "SolidWorks parts genereren", Checked = false, AutoSize = true };
            cabinetUnits = BuildCabinetUnitsGrid();
            outputFolder = new TextBox { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "WerktafelOutput"), Width = 520 };
            log = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Dock = DockStyle.Fill, Font = new Font("Consolas", 9) };

            FillProjectPage(projectPage);
            FillCabinetPage(cabinetPage);
            FillCamPage(camPage);
            FillOutputPage(outputPage);
        }

        private TabPage BuildProjectPage()
        {
            return new TabPage("Project");
        }

        private TabPage BuildCamPage()
        {
            return new TabPage("CAM");
        }

        private TabPage BuildCabinetPage()
        {
            return new TabPage("Cabinet");
        }

        private TabPage BuildOutputPage()
        {
            return new TabPage("Output");
        }

        private void FillProjectPage(Control page)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Top, Padding = new Padding(16), AutoSize = true, ColumnCount = 2 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(panel, "Productmodus", productMode);
            AddRow(panel, "Breedte mm", width);
            AddRow(panel, "Diepte mm", depth);
            AddRow(panel, "Hoogte mm", height);
            AddRow(panel, "Frameprofiel", frameProfile);
            AddRow(panel, "Bovenblad materiaal", topSheet);
            AddRow(panel, "Bladdikte mm", topThickness);
            AddRow(panel, "Legblad materiaal", shelfSheet);
            AddRow(panel, "Constructie", lowerFrame);
            AddRow(panel, "Hoogte onderframe mm", lowerFrameHeight);
            AddRow(panel, "Onderblad", lowerShelf);
            AddRow(panel, "Extra laag", middleLayer);
            AddRow(panel, "Hoogte extra laag mm", middleLayerHeight);
            AddRow(panel, "Tussenblad", middleShelf);
            AddRow(panel, "Uitsparing speling mm", shelfCornerClearance);
            AddRow(panel, "Max boutafstand mm", boltMaxSpacing);
            AddRow(panel, "Bevestiging blad", fastener);
            AddRow(panel, "Kopkamers", countersinkHoles);
            AddRow(panel, "Kopkamerdiameter mm", countersinkDiameter);
            AddRow(panel, "Kopkamerdiepte mm", countersinkDepth);

            page.Controls.Add(panel);
        }

        private void FillCabinetPage(Control page)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow4(panel, "Totale breedte mm", cabinetWidth, "Diepte mm", cabinetDepth);
            AddRow4(panel, "Hoogte blad mm", cabinetWorktopHeight, "Aantal units", cabinetUnitCount);
            AddRow4(panel, "Plinthoogte mm", cabinetPlinthHeight, "Plintdiepte mm", cabinetPlinthDepth);
            AddRow4(panel, "Romp materiaal", cabinetCarcassMaterial, "Blad materiaal", cabinetWorktopMaterial);
            AddRow4(panel, "Lade materiaal", cabinetDrawerMaterial, "Front materiaal", cabinetFrontMaterial);
            AddRow4(panel, "Achterwand materiaal", cabinetBackMaterial, "Rail-template", cabinetRailTemplate);
            AddRow4(panel, "Legplank speling mm", cabinetShelfClearance, "Lade zijdelingse speling mm", cabinetDrawerClearance);
            AddRow4(panel, "Deur/ front voeg mm", cabinetDoorGap, "Achterwand", cabinetBackPanel);

            var hint = new Label
            {
                Text = "Unit-grid: legplankhoogtes mogen leeg blijven voor automatische verdeling, of bijvoorbeeld 320;520. Deur: Geen, Links of Rechts.",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 8)
            };

            layout.Controls.Add(panel);
            layout.Controls.Add(hint);
            layout.Controls.Add(cabinetUnits);
            page.Controls.Add(layout);
        }

        private void FillCamPage(Control page)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Top, Padding = new Padding(16), AutoSize = true, ColumnCount = 2 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(panel, "Machine", new Label { Text = "Mach3 portaal 3020 x 1520, .tap, nulpunt links onder", AutoSize = true });
            AddRow(panel, "Freesdiameter mm", toolDiameter);
            AddRow(panel, "Passdiepte mm", passDepth);
            AddRow(panel, "Tabs", autoTabs);
            AddRow(panel, "Nest voorraad lengte mm", nestStockLength);
            AddRow(panel, "Nest voorraad breedte mm", nestStockWidth);
            AddRow(panel, "Nest tussenruimte mm", nestSpacing);
            AddRow(panel, "Nest randmarge mm", nestMargin);

            page.Controls.Add(panel);
        }

        private void FillOutputPage(Control page)
        {
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16) };
            top.Controls.Add(new Label { Text = "Outputmap", AutoSize = true, Padding = new Padding(0, 6, 8, 0) });
            top.Controls.Add(outputFolder);

            var browse = new Button { Text = "...", Width = 36 };
            browse.Click += delegate
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        outputFolder.Text = dialog.SelectedPath;
                    }
                }
            };
            top.Controls.Add(browse);
            top.Controls.Add(exportSolidWorks);

            var generate = new Button { Text = "Genereer output", Width = 220, Height = 32 };
            generate.Click += delegate { Generate(); };
            top.Controls.Add(generate);

            page.Controls.Add(log);
            page.Controls.Add(top);
        }

        private void Generate()
        {
            try
            {
                var isCabinet = productMode.SelectedItem != null && productMode.SelectedItem.ToString() == "Cabinet";
                var tool = BuildTool();
                var machine = BuildMachine();
                var model = isCabinet ? new CabinetEngine().Build(BuildCabinetConfig()) : new WorkbenchEngine().Build(BuildConfig());

                Directory.CreateDirectory(outputFolder.Text);

                var csv = new CsvExporter();
                File.WriteAllText(Path.Combine(outputFolder.Text, "Afkortlijst.csv"), csv.ExportCutList(model.Profiles));
                File.WriteAllText(Path.Combine(outputFolder.Text, "Boorlijst.csv"), csv.ExportDrillList(model.Profiles));
                File.WriteAllText(Path.Combine(outputFolder.Text, "Profielbewerkingen.csv"), csv.ExportProfileOperations(model.ProfileOperations));
                new ProfileOperationsXlsxExporter().Export(Path.Combine(outputFolder.Text, "Profielbewerkingen.xlsx"), model.ProfileOperations);
                File.WriteAllText(Path.Combine(outputFolder.Text, "ProfielStationPlan.txt"), csv.ExportProfileStationPlan(model));
                File.WriteAllText(Path.Combine(outputFolder.Text, "Plaatgaten.csv"), csv.ExportSheetHoleList(model.Sheets));
                File.WriteAllText(Path.Combine(outputFolder.Text, "CAM-operaties.csv"), csv.ExportCamOperations(model.Sheets, tool));
                File.WriteAllText(Path.Combine(outputFolder.Text, "ToolLibrary.csv"), csv.ExportToolLibrary(tool));
                File.WriteAllText(Path.Combine(outputFolder.Text, "BOM.csv"), csv.ExportBom(model));

                var gcode = new Mach3GCodeGenerator();
                foreach (var sheet in model.Sheets)
                {
                    var tap = gcode.GenerateSheetPart(sheet, tool, machine, sheet.Material.ThicknessMm, (double)8, (double)1.5);
                    File.WriteAllText(Path.Combine(outputFolder.Text, sheet.Name + ".tap"), tap);
                }

                var nestingFolder = Path.Combine(outputFolder.Text, "Nesting");
                Directory.CreateDirectory(nestingFolder);
                var nestingPlan = new SheetNestingEngine().Build(model, machine, (double)nestSpacing.Value, (double)nestMargin.Value, (double)nestStockLength.Value, (double)nestStockWidth.Value);
                var nestingExporter = new NestingExporter();
                File.WriteAllText(Path.Combine(nestingFolder, "NestPlan.csv"), nestingExporter.ExportCsv(nestingPlan));
                File.WriteAllText(Path.Combine(nestingFolder, "NestVisualisatie.svg"), nestingExporter.ExportSvg(nestingPlan));
                var nestedGcode = new NestedMach3GCodeGenerator();
                foreach (var stock in nestingPlan.StockSheets)
                {
                    File.WriteAllText(Path.Combine(nestingFolder, stock.Name + ".tap"), nestedGcode.Generate(stock, tool, machine));
                }

                var plan = SolidWorksExportPlan.FromWorkbench(model);
                File.WriteAllText(Path.Combine(outputFolder.Text, "SolidWorksExportPlan.txt"), FormatPlan(plan));

                var solidWorksLine = "";
                if (exportSolidWorks.Checked)
                {
                    var macroPath = new SolidWorksMacroExporter().ExportMacro(model, outputFolder.Text);
                    try
                    {
                        new SolidWorksComPartExporter().ExportParts(model, outputFolder.Text);
                        solidWorksLine = "- SolidWorks\\*.SLDPRT" + Environment.NewLine
                            + "- Maak_*_Parts.bas (fallback macro)" + Environment.NewLine;
                    }
                    catch (Exception swEx)
                    {
                        solidWorksLine = "- Maak_*_Parts.bas" + Environment.NewLine
                            + "- SolidWorksMacroInstructies.txt" + Environment.NewLine
                            + Environment.NewLine
                            + "Directe SolidWorks-koppeling niet beschikbaar:" + Environment.NewLine
                            + swEx.Message + Environment.NewLine
                            + Environment.NewLine
                            + "Macro gegenereerd: " + macroPath + Environment.NewLine;
                    }
                }

                log.Text = "Gegenereerd in: " + outputFolder.Text + Environment.NewLine + Environment.NewLine
                    + "Bestanden:" + Environment.NewLine
                    + "- Afkortlijst.csv" + Environment.NewLine
                    + "- Boorlijst.csv" + Environment.NewLine
                    + "- Profielbewerkingen.csv" + Environment.NewLine
                    + "- Profielbewerkingen.xlsx" + Environment.NewLine
                    + "- ProfielStationPlan.txt" + Environment.NewLine
                    + "- Plaatgaten.csv" + Environment.NewLine
                    + "- CAM-operaties.csv" + Environment.NewLine
                    + "- ToolLibrary.csv" + Environment.NewLine
                    + "- BOM.csv" + Environment.NewLine
                    + "- Plaatdelen .tap" + Environment.NewLine
                    + "- Nesting\\NestPlan.csv" + Environment.NewLine
                    + "- Nesting\\NestVisualisatie.svg" + Environment.NewLine
                    + "- Nesting\\*.tap" + Environment.NewLine
                    + solidWorksLine
                    + "- SolidWorksExportPlan.txt" + Environment.NewLine;
            }
            catch (Exception ex)
            {
                log.Text = ex.Message;
            }
        }

        private CabinetConfig BuildCabinetConfig()
        {
            var config = new CabinetConfig
            {
                ProjectName = "Cabinet_" + cabinetWidth.Value.ToString("0") + "x" + cabinetDepth.Value.ToString("0") + "x" + cabinetWorktopHeight.Value.ToString("0"),
                WidthMm = (double)cabinetWidth.Value,
                DepthMm = (double)cabinetDepth.Value,
                WorktopHeightMm = (double)cabinetWorktopHeight.Value,
                UnitCount = (int)cabinetUnitCount.Value,
                PlinthHeightMm = (double)cabinetPlinthHeight.Value,
                PlinthDepthMm = (double)cabinetPlinthDepth.Value,
                IncludeBackPanel = cabinetBackPanel.Checked,
                CarcassMaterial = CloneMaterial((Material)cabinetCarcassMaterial.SelectedItem),
                WorktopMaterial = CloneMaterial((Material)cabinetWorktopMaterial.SelectedItem),
                DrawerMaterial = CloneMaterial((Material)cabinetDrawerMaterial.SelectedItem),
                FrontMaterial = CloneMaterial((Material)cabinetFrontMaterial.SelectedItem),
                BackMaterial = CloneMaterial((Material)cabinetBackMaterial.SelectedItem),
                SheetFastener = CloneFastener((FastenerDefinition)fastener.SelectedItem),
                DrawerRail = CloneRail((RailTemplate)cabinetRailTemplate.SelectedItem),
                AutoTabs = autoTabs.Checked,
                SmallPartAreaThresholdMm2 = 300 * 300,
                TabWidthMm = 8,
                TabHeightMm = 1.5,
                ShelfClearanceMm = (double)cabinetShelfClearance.Value,
                DrawerSideClearanceMm = (double)cabinetDrawerClearance.Value,
                DoorGapMm = (double)cabinetDoorGap.Value
            };

            foreach (DataGridViewRow row in cabinetUnits.Rows)
            {
                if (row.IsNewRow) continue;
                var unitNumber = CellInt(row, 0, 0);
                if (unitNumber <= 0 || unitNumber > config.UnitCount) continue;
                config.Units.Add(new CabinetUnitConfig
                {
                    UnitNumber = unitNumber,
                    ShelfCount = CellInt(row, 1, 0),
                    ShelfHeightsMm = CellText(row, 2),
                    DrawerCount = CellInt(row, 3, 0),
                    DrawerHeightMm = CellDouble(row, 4, 160),
                    Door = ParseDoor(CellText(row, 5)),
                    SlidingDoors = CellBool(row, 6),
                    SlidingDoorMaxWidthMm = CellDouble(row, 7, 600)
                });
            }

            return config;
        }

        private WorkbenchConfig BuildConfig()
        {
            var selectedTopSheet = CloneMaterial((Material)topSheet.SelectedItem);
            selectedTopSheet.ThicknessMm = (double)topThickness.Value;

            return new WorkbenchConfig
            {
                ProjectName = "Werktafel_" + width.Value.ToString("0") + "x" + depth.Value.ToString("0") + "x" + height.Value.ToString("0"),
                WidthMm = (double)width.Value,
                DepthMm = (double)depth.Value,
                HeightMm = (double)height.Value,
                FrameProfile = CloneMaterial((Material)frameProfile.SelectedItem),
                TopSheet = selectedTopSheet,
                ShelfSheet = CloneMaterial((Material)shelfSheet.SelectedItem),
                IncludeLowerFrame = lowerFrame.Checked,
                LowerFrameHeightMm = (double)lowerFrameHeight.Value,
                IncludeLowerShelf = lowerShelf.Checked,
                IncludeMiddleLayer = middleLayer.Checked,
                MiddleLayerHeightMm = (double)middleLayerHeight.Value,
                IncludeMiddleShelf = middleShelf.Checked,
                ShelfCornerClearanceMm = (double)shelfCornerClearance.Value,
                BoltMaxSpacingMm = (double)boltMaxSpacing.Value,
                TopOverhangFrontMm = 0,
                TopOverhangBackMm = 0,
                TopOverhangLeftMm = 0,
                TopOverhangRightMm = 0,
                SheetFastener = CloneFastener((FastenerDefinition)fastener.SelectedItem),
                ConnectorHoleDiameterMm = ((FastenerDefinition)fastener.SelectedItem).ClearanceHoleDiameterMm,
                CountersinkSheetHoles = countersinkHoles.Checked,
                CountersinkDiameterMm = ((FastenerDefinition)fastener.SelectedItem).CounterboreDiameterMm,
                CountersinkDepthMm = ((FastenerDefinition)fastener.SelectedItem).CounterboreDepthMm,
                AutoTabs = autoTabs.Checked,
                SmallPartAreaThresholdMm2 = 300 * 300,
                TabWidthMm = 8,
                TabHeightMm = 1.5
            };
        }

        private ToolDefinition BuildTool()
        {
            return LibraryCatalog.DefaultEndMill((double)toolDiameter.Value, (double)passDepth.Value);
        }

        private static MachineProfile BuildMachine()
        {
            return new MachineProfile
            {
                Id = "mach3_portaal_3020x1520",
                Name = "Mach3 portaalfrees 3020x1520",
                MaxXmm = 3020,
                MaxYmm = 1520,
                FileExtension = ".tap",
                SafeZmm = 15,
                Origin = "links onder"
            };
        }

        private static string FormatPlan(SolidWorksExportPlan plan)
        {
            var text = "Assembly: " + plan.AssemblyName + Environment.NewLine + "Parts:" + Environment.NewLine;
            foreach (var part in plan.PartNames)
            {
                text += "- " + part + Environment.NewLine;
            }

            return text;
        }

        private static NumericUpDown Number(decimal value, decimal min, decimal max)
        {
            var number = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = value % 1 == 0 ? 0 : 1,
                Increment = value % 1 == 0 ? 10 : 0.5m,
                Width = 120
            };
            number.Value = value;
            return number;
        }

        private static ComboBox MaterialCombo(Material[] materials)
        {
            return MaterialCombo(materials, 0);
        }

        private static ComboBox MaterialCombo(Material[] materials, int selectedIndex)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220,
                DisplayMember = "Name"
            };
            combo.Items.AddRange(materials);
            combo.SelectedIndex = selectedIndex;
            return combo;
        }

        private static ComboBox FastenerCombo(FastenerDefinition[] fasteners, int selectedIndex)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                DisplayMember = "Name"
            };
            combo.Items.AddRange(fasteners);
            combo.SelectedIndex = selectedIndex;
            return combo;
        }

        private static ComboBox RailCombo(RailTemplate[] rails, int selectedIndex)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                DisplayMember = "Name"
            };
            combo.Items.AddRange(rails);
            combo.SelectedIndex = selectedIndex;
            return combo;
        }

        private static DataGridView BuildCabinetUnitsGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersWidth = 28
            };
            grid.Columns.Add("Unit", "Unit");
            grid.Columns.Add("Shelves", "Legplanken");
            grid.Columns.Add("ShelfHeights", "Legplank hoogtes");
            grid.Columns.Add("Drawers", "Lades");
            grid.Columns.Add("DrawerHeight", "Ladehoogte");
            grid.Columns.Add("Door", "Deur");
            grid.Columns.Add("Sliding", "Schuifdeuren");
            grid.Columns.Add("SlidingMax", "Max schuifdeurbreedte");

            grid.Rows.Add("1", "1", "", "0", "160", "Geen", "nee", "600");
            grid.Rows.Add("2", "0", "", "3", "160", "Geen", "nee", "600");
            grid.Rows.Add("3", "2", "", "0", "160", "Links", "nee", "600");
            grid.Rows.Add("4", "0", "", "0", "160", "Geen", "ja", "600");
            return grid;
        }

        private static FastenerDefinition CloneFastener(FastenerDefinition fastener)
        {
            return new FastenerDefinition
            {
                Id = fastener.Id,
                Name = fastener.Name,
                Standard = fastener.Standard,
                NominalDiameterMm = fastener.NominalDiameterMm,
                ClearanceHoleDiameterMm = fastener.ClearanceHoleDiameterMm,
                HeadKind = fastener.HeadKind,
                HeadDiameterMm = fastener.HeadDiameterMm,
                HeadHeightMm = fastener.HeadHeightMm,
                HeadClearanceMm = fastener.HeadClearanceMm
            };
        }

        private static RailTemplate CloneRail(RailTemplate rail)
        {
            return new RailTemplate
            {
                Id = rail.Id,
                Name = rail.Name,
                LengthMm = rail.LengthMm,
                HoleCount = rail.HoleCount,
                FirstHoleOffsetMm = rail.FirstHoleOffsetMm,
                HoleSpacingMm = rail.HoleSpacingMm,
                VerticalOffsetMm = rail.VerticalOffsetMm,
                HoleDiameterMm = rail.HoleDiameterMm,
                FastenerName = rail.FastenerName
            };
        }

        private static Material CloneMaterial(Material material)
        {
            return new Material
            {
                Id = material.Id,
                Name = material.Name,
                Kind = material.Kind,
                WidthMm = material.WidthMm,
                HeightMm = material.HeightMm,
                ThicknessMm = material.ThicknessMm,
                StockLengthMm = material.StockLengthMm,
                SheetLengthMm = material.SheetLengthMm,
                SheetWidthMm = material.SheetWidthMm
            };
        }

        private static void AddRow(TableLayoutPanel panel, string label, Control control)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            panel.Controls.Add(control);
        }

        private static void AddRow4(TableLayoutPanel panel, string label1, Control control1, string label2, Control control2)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(new Label { Text = label1, AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            panel.Controls.Add(control1);
            panel.Controls.Add(new Label { Text = label2, AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            panel.Controls.Add(control2);
        }

        private static int CellInt(DataGridViewRow row, int index, int defaultValue)
        {
            int value;
            if (int.TryParse(CellText(row, index), out value)) return value;
            return defaultValue;
        }

        private static double CellDouble(DataGridViewRow row, int index, double defaultValue)
        {
            double value;
            var text = CellText(row, index).Replace(',', '.');
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value)) return value;
            return defaultValue;
        }

        private static bool CellBool(DataGridViewRow row, int index)
        {
            var text = CellText(row, index).Trim().ToLowerInvariant();
            return text == "ja" || text == "yes" || text == "true" || text == "1";
        }

        private static string CellText(DataGridViewRow row, int index)
        {
            if (index >= row.Cells.Count || row.Cells[index].Value == null) return "";
            return row.Cells[index].Value.ToString();
        }

        private static CabinetDoorHand ParseDoor(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            if (value == "links" || value == "left") return CabinetDoorHand.Links;
            if (value == "rechts" || value == "right") return CabinetDoorHand.Rechts;
            return CabinetDoorHand.Geen;
        }
    }
}
