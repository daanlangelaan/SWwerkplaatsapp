using System;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Engine
{
    public sealed class WorkbenchEngine
    {
        public WorkbenchModel Build(WorkbenchConfig config)
        {
            Validate(config);

            var profile = config.FrameProfile;
            var fastener = config.SheetFastener;
            var profileSize = profile.WidthMm;
            var topThickness = config.TopSheet.ThicknessMm;
            var shelfMaterial = config.ShelfSheet ?? config.TopSheet;
            var model = new WorkbenchModel
            {
                ProjectName = config.ProjectName,
                SheetFastener = fastener,
                LowerFrameHeightMm = config.LowerFrameHeightMm,
                MiddleLayerHeightMm = config.MiddleLayerHeightMm
            };

            var legLength = config.HeightMm - topThickness;
            var frontBackLength = config.WidthMm - 2.0 * profileSize;
            var sideLength = config.DepthMm - 2.0 * profileSize;

            var legPart = Profile("Poot", profile, legLength, 4, "Kop A onder, kop B boven", config);
            AddLegFrameConnectionDrills(legPart, legLength - profileSize / 2.0, config);
            if (config.IncludeLowerFrame || config.IncludeLowerShelf)
            {
                AddLegFrameConnectionDrills(legPart, config.LowerFrameHeightMm, config);
            }

            if (config.IncludeMiddleLayer || config.IncludeMiddleShelf)
            {
                AddLegFrameConnectionDrills(legPart, config.MiddleLayerHeightMm, config);
            }

            legPart.Drills.Add(new DrillOperation
            {
                Side = "Kop B centrum",
                PositionFromEndAMm = legLength,
                DiameterMm = fastener.NominalDiameterMm,
                ThroughHole = false,
                Note = fastener.Name + " draad tappen voor bladmontage op kopse staander"
            });
            model.Profiles.Add(legPart);
            model.Profiles.Add(TappedFrameProfile("Bovenframe voor/achter", profile, frontBackLength, 2, "Lengte over X-as", config));
            model.Profiles.Add(TappedFrameProfile("Bovenframe links/rechts", profile, sideLength, 2, "Lengte over Y-as", config));

            if (config.IncludeLowerFrame || config.IncludeLowerShelf)
            {
                model.Profiles.Add(TappedFrameProfile("Onderframe voor/achter", profile, frontBackLength, 2, "Lengte over X-as", config));
                model.Profiles.Add(TappedFrameProfile("Onderframe links/rechts", profile, sideLength, 2, "Lengte over Y-as", config));
            }

            if (config.IncludeMiddleLayer || config.IncludeMiddleShelf)
            {
                model.Profiles.Add(TappedFrameProfile("Tussenframe voor/achter", profile, frontBackLength, 2, "Lengte over X-as", config));
                model.Profiles.Add(TappedFrameProfile("Tussenframe links/rechts", profile, sideLength, 2, "Lengte over Y-as", config));
            }

            var top = new SheetPart
            {
                Name = "Werkblad",
                Material = config.TopSheet,
                LengthMm = config.WidthMm + config.TopOverhangLeftMm + config.TopOverhangRightMm,
                WidthMm = config.DepthMm + config.TopOverhangFrontMm + config.TopOverhangBackMm,
                Quantity = 1,
                CenterHeightMm = legLength + topThickness / 2.0
            };

            top.UseTabs = config.AutoTabs && top.LengthMm * top.WidthMm < config.SmallPartAreaThresholdMm2;
            AddTopMountingHoles(top, config, profileSize);
            model.Sheets.Add(top);

            if (config.IncludeLowerShelf)
            {
                model.Sheets.Add(Shelf("Onderblad", shelfMaterial, config, config.LowerFrameHeightMm + profileSize / 2.0 + shelfMaterial.ThicknessMm / 2.0));
            }

            if (config.IncludeMiddleShelf)
            {
                model.Sheets.Add(Shelf("Tussenblad", shelfMaterial, config, config.MiddleLayerHeightMm + profileSize / 2.0 + shelfMaterial.ThicknessMm / 2.0));
            }

            BuildProfileOperations(model);
            AddHardware(model);
            return model;
        }

        private static void BuildProfileOperations(WorkbenchModel model)
        {
            foreach (var profile in model.Profiles)
            {
                var profileId = ProfileId(profile);
                var sequence = 1;
                model.ProfileOperations.Add(new ProfileOperation
                {
                    ProfileId = profileId,
                    PartName = profile.Name,
                    Quantity = profile.Quantity,
                    Material = profile.Material,
                    ProfileLengthMm = profile.LengthMm,
                    Sequence = sequence++,
                    Kind = ProfileOperationKind.SawCut,
                    Side = "",
                    PositionFromEndAMm = 0,
                    DiameterMm = 0,
                    ThroughHole = false,
                    SawAngleDeg = 90,
                    WorkOrigin = "Kop A",
                    MachineHint = "SAW_CUT",
                    Note = profile.OrientationNote
                });

                foreach (var drill in profile.Drills)
                {
                    var kind = drill.ThroughHole ? ProfileOperationKind.Drill : ProfileOperationKind.Tap;
                    model.ProfileOperations.Add(new ProfileOperation
                    {
                        ProfileId = profileId,
                        PartName = profile.Name,
                        Quantity = profile.Quantity,
                        Material = profile.Material,
                        ProfileLengthMm = profile.LengthMm,
                        Sequence = sequence++,
                        Kind = kind,
                        Side = drill.Side,
                        PositionFromEndAMm = drill.PositionFromEndAMm,
                        DiameterMm = drill.DiameterMm,
                        ThroughHole = drill.ThroughHole,
                        SawAngleDeg = 0,
                        WorkOrigin = "Kop A",
                        MachineHint = kind == ProfileOperationKind.Drill ? "DRILL" : "TAP",
                        Note = drill.Note
                    });
                }
            }
        }

        private static void AddHardware(WorkbenchModel model)
        {
            var sheetHoleCount = 0;
            var profileNutHoleCount = 0;
            var countersunkHoleCount = 0;
            foreach (var sheet in model.Sheets)
            {
                foreach (var hole in sheet.Holes)
                {
                    sheetHoleCount += sheet.Quantity;
                    if (hole.Countersunk)
                    {
                        countersunkHoleCount += sheet.Quantity;
                    }

                    if (hole.SupportKind == SheetHoleSupportKind.ProfileNut)
                    {
                        profileNutHoleCount += sheet.Quantity;
                    }
                }
            }

            if (countersunkHoleCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = model.SheetFastener.Name,
                    ArticleNumber = model.SheetFastener.Id,
                    Quantity = countersunkHoleCount,
                    Unit = "st",
                    Note = model.SheetFastener.Standard + "; kopdiameter " + model.SheetFastener.HeadDiameterMm.ToString("0.##") + " mm, kophoogte " + model.SheetFastener.HeadHeightMm.ToString("0.##") + " mm"
                });
            }

            if (sheetHoleCount - countersunkHoleCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = model.SheetFastener.Name,
                    ArticleNumber = model.SheetFastener.Id,
                    Quantity = sheetHoleCount - countersunkHoleCount,
                    Unit = "st",
                    Note = "1 per vlak plaatmontagegat"
                });
            }

            if (sheetHoleCount > 0)
            {
                if (countersunkHoleCount > 0)
                {
                    model.Hardware.Add(new HardwareItem
                    {
                        Name = "Kopkamerbewerking plaatgaten",
                        ArticleNumber = "KOPKAMER_M8_ISO4762",
                        Quantity = countersunkHoleCount,
                        Unit = "st",
                        Note = "Vlakke kopkamer volgens kopdiameter/kophoogte bout"
                    });
                }

                model.Hardware.Add(new HardwareItem
                {
                    Name = model.SheetFastener.NominalDiameterMm.ToString("0") + "mm ring optioneel onder profielzijde",
                    ArticleNumber = "RING_" + model.SheetFastener.NominalDiameterMm.ToString("0"),
                    Quantity = sheetHoleCount,
                    Unit = "st",
                    Note = countersunkHoleCount == sheetHoleCount ? "Optioneel; niet onder verzonken kop aan plaatzijde" : "1 per plaatmontagegat"
                });
            }

            if (sheetHoleCount - profileNutHoleCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = model.SheetFastener.NominalDiameterMm.ToString("0") + "mm draad tappen in kopse staander",
                    ArticleNumber = "TAP_M" + model.SheetFastener.NominalDiameterMm.ToString("0"),
                    Quantity = sheetHoleCount - profileNutHoleCount,
                    Unit = "st",
                    Note = "Voor bladgaten boven staandercenters"
                });
            }

            if (profileNutHoleCount > 0)
            {
                model.Hardware.Add(new HardwareItem
                {
                    Name = "M" + model.SheetFastener.NominalDiameterMm.ToString("0") + " T-moer / profielmoer",
                    ArticleNumber = "M" + model.SheetFastener.NominalDiameterMm.ToString("0") + "_TMOER",
                    Quantity = profileNutHoleCount,
                    Unit = "st",
                    Note = "1 per plaatmontagegat op profiel"
                });
            }
        }

        private static SheetPart Shelf(string name, Material material, WorkbenchConfig config, double centerHeight)
        {
            var profileSize = config.FrameProfile.WidthMm;
            var notch = profileSize + config.ShelfCornerClearanceMm;
            var shelf = new SheetPart
            {
                Name = name,
                Material = material,
                LengthMm = config.WidthMm,
                WidthMm = config.DepthMm,
                Quantity = 1,
                CenterHeightMm = centerHeight,
                HasCornerNotches = true,
                CornerNotchSizeMm = notch
            };

            shelf.UseTabs = config.AutoTabs && shelf.LengthMm * shelf.WidthMm < config.SmallPartAreaThresholdMm2;
            AddShelfMountingHoles(shelf, config, profileSize);
            ApplyCountersinks(shelf, config);
            return shelf;
        }

        private static ProfilePart Profile(string name, Material material, double length, int quantity, string note, WorkbenchConfig config)
        {
            var part = new ProfilePart
            {
                Name = name,
                Material = material,
                LengthMm = Math.Round(length, 2),
                Quantity = quantity,
                OrientationNote = note
            };

            return part;
        }

        private static ProfilePart TappedFrameProfile(string name, Material material, double length, int quantity, string note, WorkbenchConfig config)
        {
            var part = Profile(name, material, length, quantity, note, config);
            part.Drills.Add(new DrillOperation
            {
                Side = "Kop A centrum",
                PositionFromEndAMm = 0,
                DiameterMm = config.SheetFastener.NominalDiameterMm,
                ThroughHole = false,
                Note = config.SheetFastener.Name + " draad tappen voor koppeling aan staander"
            });
            part.Drills.Add(new DrillOperation
            {
                Side = "Kop B centrum",
                PositionFromEndAMm = Math.Round(length, 2),
                DiameterMm = config.SheetFastener.NominalDiameterMm,
                ThroughHole = false,
                Note = config.SheetFastener.Name + " draad tappen voor koppeling aan staander"
            });

            return part;
        }

        private static void AddLegFrameConnectionDrills(ProfilePart legPart, double centerHeightMm, WorkbenchConfig config)
        {
            var position = Math.Round(centerHeightMm, 2);
            legPart.Drills.Add(new DrillOperation
            {
                Side = "X-zijde centrum",
                PositionFromEndAMm = position,
                DiameterMm = config.ConnectorHoleDiameterMm,
                ThroughHole = true,
                Note = "Doorboren staander op hart links/rechts ligger"
            });
            legPart.Drills.Add(new DrillOperation
            {
                Side = "Z-zijde centrum",
                PositionFromEndAMm = position,
                DiameterMm = config.ConnectorHoleDiameterMm,
                ThroughHole = true,
                Note = "Doorboren staander op hart voor/achter ligger"
            });
        }

        private static void AddTopMountingHoles(SheetPart top, WorkbenchConfig config, double profileSize)
        {
            var x1 = config.TopOverhangLeftMm + profileSize / 2.0;
            var y1 = config.TopOverhangFrontMm + profileSize / 2.0;
            var x2 = top.LengthMm - config.TopOverhangRightMm - profileSize / 2.0;
            var y2 = top.WidthMm - config.TopOverhangBackMm - profileSize / 2.0;

            AddUniqueHole(top, x1, y1, config.ConnectorHoleDiameterMm, "kopse staander LV", SheetHoleSupportKind.TappedProfileEnd);
            AddUniqueHole(top, x2, y1, config.ConnectorHoleDiameterMm, "kopse staander RV", SheetHoleSupportKind.TappedProfileEnd);
            AddUniqueHole(top, x1, y2, config.ConnectorHoleDiameterMm, "kopse staander LA", SheetHoleSupportKind.TappedProfileEnd);
            AddUniqueHole(top, x2, y2, config.ConnectorHoleDiameterMm, "kopse staander RA", SheetHoleSupportKind.TappedProfileEnd);
            AddPerimeterBoltPattern(top, x1, y1, x2, y2, config.ConnectorHoleDiameterMm, config.BoltMaxSpacingMm);
            ApplyCountersinks(top, config);
        }

        private static void AddShelfMountingHoles(SheetPart shelf, WorkbenchConfig config, double profileSize)
        {
            var notch = profileSize + config.ShelfCornerClearanceMm;
            var lineInset = profileSize / 2.0;
            var x1OnFrontBack = notch + lineInset;
            var x2OnFrontBack = shelf.LengthMm - notch - lineInset;
            var y1OnLeftRight = notch + lineInset;
            var y2OnLeftRight = shelf.WidthMm - notch - lineInset;
            var frontY = lineInset;
            var backY = shelf.WidthMm - lineInset;
            var leftX = lineInset;
            var rightX = shelf.LengthMm - lineInset;

            AddBoltLine(shelf, x1OnFrontBack, frontY, x2OnFrontBack, frontY, config.ConnectorHoleDiameterMm, config.BoltMaxSpacingMm, "voorprofiel");
            AddBoltLine(shelf, x1OnFrontBack, backY, x2OnFrontBack, backY, config.ConnectorHoleDiameterMm, config.BoltMaxSpacingMm, "achterprofiel");
            AddBoltLine(shelf, leftX, y1OnLeftRight, leftX, y2OnLeftRight, config.ConnectorHoleDiameterMm, config.BoltMaxSpacingMm, "linkerprofiel");
            AddBoltLine(shelf, rightX, y1OnLeftRight, rightX, y2OnLeftRight, config.ConnectorHoleDiameterMm, config.BoltMaxSpacingMm, "rechterprofiel");
        }

        private static void AddPerimeterBoltPattern(SheetPart sheet, double x1, double y1, double x2, double y2, double diameter, double maxSpacing)
        {
            AddBoltLine(sheet, x1, y1, x2, y1, diameter, maxSpacing, "voorprofiel");
            AddBoltLine(sheet, x1, y2, x2, y2, diameter, maxSpacing, "achterprofiel");
            AddBoltLine(sheet, x1, y1, x1, y2, diameter, maxSpacing, "linkerprofiel");
            AddBoltLine(sheet, x2, y1, x2, y2, diameter, maxSpacing, "rechterprofiel");
        }

        private static void AddBoltLine(SheetPart sheet, double x1, double y1, double x2, double y2, double diameter, double maxSpacing, string note)
        {
            var length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            if (length < 0.001)
            {
                AddUniqueHole(sheet, x1, y1, diameter, note);
                return;
            }

            var safeSpacing = Math.Max(1, maxSpacing);
            var segments = Math.Max(1, (int)Math.Ceiling(length / safeSpacing));

            for (var i = 0; i <= segments; i++)
            {
                var t = (double)i / segments;
                AddUniqueHole(
                    sheet,
                    x1 + (x2 - x1) * t,
                    y1 + (y2 - y1) * t,
                    diameter,
                    note);
            }
        }

        private static void AddUniqueHole(SheetPart sheet, double x, double y, double diameter, string note)
        {
            AddUniqueHole(sheet, x, y, diameter, note, SheetHoleSupportKind.ProfileNut);
        }

        private static void AddUniqueHole(SheetPart sheet, double x, double y, double diameter, string note, SheetHoleSupportKind supportKind)
        {
            x = Math.Round(x, 3);
            y = Math.Round(y, 3);

            foreach (var hole in sheet.Holes)
            {
                if (Math.Abs(hole.Xmm - x) < 0.01 && Math.Abs(hole.Ymm - y) < 0.01)
                {
                    return;
                }
            }

            sheet.Holes.Add(new SheetHole
            {
                Name = "Montagegat " + note + " " + (sheet.Holes.Count + 1),
                Xmm = x,
                Ymm = y,
                DiameterMm = diameter,
                Countersunk = false,
                CountersinkDiameterMm = 0,
                CountersinkDepthMm = 0,
                SupportKind = supportKind
            });
        }

        private static void ApplyCountersinks(SheetPart sheet, WorkbenchConfig config)
        {
            if (!config.CountersinkSheetHoles) return;

            var depth = Math.Min(config.CountersinkDepthMm, Math.Max(0.1, sheet.Material.ThicknessMm - 0.1));
            foreach (var hole in sheet.Holes)
            {
                hole.Countersunk = true;
                hole.CountersinkDiameterMm = config.CountersinkDiameterMm;
                hole.CountersinkDepthMm = depth;
            }
        }

        private static string ProfileId(ProfilePart profile)
        {
            return profile.Name.Replace(" ", "_").Replace("/", "-") + "_" + profile.LengthMm.ToString("0.##") + "mm";
        }

        private static void Validate(WorkbenchConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (config.FrameProfile == null) throw new ArgumentException("FrameProfile ontbreekt.");
            if (config.TopSheet == null) throw new ArgumentException("TopSheet ontbreekt.");
            if (config.ShelfSheet == null) config.ShelfSheet = config.TopSheet;
            if (config.SheetFastener == null) throw new ArgumentException("Bevestigingstype ontbreekt.");
            config.ConnectorHoleDiameterMm = config.SheetFastener.ClearanceHoleDiameterMm;
            config.CountersinkDiameterMm = config.SheetFastener.CounterboreDiameterMm;
            config.CountersinkDepthMm = config.SheetFastener.CounterboreDepthMm;
            if (config.WidthMm <= 0 || config.DepthMm <= 0 || config.HeightMm <= 0) throw new ArgumentException("Afmetingen moeten groter zijn dan 0.");
            if (config.HeightMm <= config.TopSheet.ThicknessMm) throw new ArgumentException("Hoogte moet groter zijn dan de bladdikte.");
            if (config.BoltMaxSpacingMm <= 0) throw new ArgumentException("Max boutafstand moet groter zijn dan 0.");
            if (config.CountersinkSheetHoles && config.CountersinkDiameterMm <= config.ConnectorHoleDiameterMm) throw new ArgumentException("Verzinkdiameter moet groter zijn dan gatdiameter.");
            if (config.CountersinkSheetHoles && config.CountersinkDepthMm <= 0) throw new ArgumentException("Verzinkdiepte moet groter zijn dan 0.");
            if (config.LowerFrameHeightMm < 0 || config.LowerFrameHeightMm >= config.HeightMm) throw new ArgumentException("Hoogte onderframe moet binnen de tafelhoogte liggen.");
            if (config.IncludeMiddleLayer && (config.MiddleLayerHeightMm <= config.LowerFrameHeightMm || config.MiddleLayerHeightMm >= config.HeightMm)) throw new ArgumentException("Hoogte extra laag moet tussen onderframe en bovenblad liggen.");
        }
    }
}
