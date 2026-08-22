using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SWWerkplaats.Configurator.Application;
using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Manufacturing
{
    public sealed class PlinthClipAdapterExportService
    {
        public List<string> ExportOpenScad(WorkbenchCabinetConfig config, string outputFolder)
        {
            if (config == null) throw new ArgumentNullException("config");
            if (string.IsNullOrWhiteSpace(outputFolder)) throw new ArgumentException("Outputmap ontbreekt.");

            var foot = config.AdjustableFoot ?? ProductDefaults.WorkbenchCabinetAdjustableFoot();
            var adapter = foot.PlinthClipAdapter ?? ProductDefaults.WorkbenchCabinetPlinthClipAdapter();
            var printFolder = Path.Combine(outputFolder, "3D-print");
            Directory.CreateDirectory(printFolder);
            var files = new List<string>();

            Write(files, printFolder, outputFolder, "SEKTION_plintclip_adapter_voor_v2_vleugel_rechts.scad", BuildOpenScad(adapter, ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config), "voorplint, vleugel rechts", 1.0));
            Write(files, printFolder, outputFolder, "SEKTION_plintclip_adapter_voor_v2_vleugel_links.scad", BuildOpenScad(adapter, ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config), "voorplint, vleugel links", -1.0));
            if (config.IncludeLeftSidePlinth || config.IncludeRightSidePlinth)
            {
                Write(files, printFolder, outputFolder, "SEKTION_plintclip_adapter_zijde_v2_vleugel_rechts.scad", BuildOpenScad(adapter, ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config), "zijplint, vleugel rechts", 1.0));
                Write(files, printFolder, outputFolder, "SEKTION_plintclip_adapter_zijde_v2_vleugel_links.scad", BuildOpenScad(adapter, ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config), "zijplint, vleugel links", -1.0));
            }
            Write(files, printFolder, outputFolder, "LEESMIJ_plintclip_adapters.txt", BuildReadMe(config, adapter));
            return files;
        }

        public static string BuildOpenScad(PlinthClipAdapterTemplate adapter, double standOffMm, string variant, double wingSign)
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("// Parametrische IKEA SEKTION plintclip-adapter - " + variant);
            sb.AppendLine("// Gegenereerd door SWWerkplaats.Configurator; maten in mm.");
            sb.AppendLine("tong_w=" + adapter.TongueWidthMm.ToString("0.###", c) + ";");
            sb.AppendLine("tong_h=" + adapter.TongueHeightMm.ToString("0.###", c) + ";");
            sb.AppendLine("tong_d=" + adapter.TongueThicknessMm.ToString("0.###", c) + ";");
            sb.AppendLine("clearance=" + adapter.PrintClearancePerSideMm.ToString("0.###", c) + ";");
            sb.AppendLine("slot_w=tong_w+2*clearance;");
            sb.AppendLine("slot_h=tong_h+2*clearance;");
            sb.AppendLine("slot_d=tong_d+2*clearance;");
            sb.AppendLine("plate_w=" + adapter.BackPlateWidthMm.ToString("0.###", c) + ";");
            sb.AppendLine("plate_h=" + adapter.BackPlateHeightMm.ToString("0.###", c) + ";");
            sb.AppendLine("wing_ext=" + adapter.MountingWingExtensionMm.ToString("0.###", c) + ";");
            sb.AppendLine("wing_sign=" + (wingSign < 0 ? "-1" : "1") + ";");
            sb.AppendLine("upper_hole_x=wing_sign*" + adapter.UpperMountingHoleHorizontalOffsetMm.ToString("0.###", c) + ";");
            sb.AppendLine("plate_x0=-plate_w/2+min(0,wing_sign*wing_ext);");
            sb.AppendLine("stand_off=" + standOffMm.ToString("0.###", c) + ";");
            sb.AppendLine("lip_overlap=" + adapter.GuideLipOverlapMm.ToString("0.###", c) + ";");
            sb.AppendLine("lip_d=" + adapter.GuideLipThicknessMm.ToString("0.###", c) + ";");
            sb.AppendLine("hole_d=" + adapter.MountingHoleDiameterMm.ToString("0.###", c) + ";");
            sb.AppendLine("hole_spacing=" + adapter.MountingHoleSpacingMm.ToString("0.###", c) + ";");
            sb.AppendLine("csink_d=" + adapter.MountingCountersinkDiameterMm.ToString("0.###", c) + ";");
            sb.AppendLine("csink_depth=" + adapter.MountingCountersinkDepthMm.ToString("0.###", c) + ";");
            sb.AppendLine("total_d=stand_off+slot_d+lip_d;");
            sb.AppendLine("$fn=64;");
            sb.AppendLine();
            sb.AppendLine("difference(){");
            sb.AppendLine("  translate([plate_x0,-plate_h/2,0]) cube([plate_w+wing_ext,plate_h,total_d]);");
            sb.AppendLine("  translate([-slot_w/2,-slot_h/2,stand_off]) cube([slot_w,plate_h,slot_d+0.01]);");
            sb.AppendLine("  translate([-(slot_w-2*lip_overlap)/2,-slot_h/2,stand_off+slot_d]) cube([slot_w-2*lip_overlap,plate_h,lip_d+0.01]);");
            sb.AppendLine("  translate([0,-hole_spacing/2,-0.1]) cylinder(d=hole_d,h=total_d+0.2);");
            sb.AppendLine("  translate([upper_hole_x,hole_spacing/2,-0.1]) cylinder(d=hole_d,h=total_d+0.2);");
            sb.AppendLine("  // Onderste schroef verzinkt vanaf het vrije buitenvlak van de adapter.");
            sb.AppendLine("  translate([0,-hole_spacing/2,total_d-csink_depth-0.01]) cylinder(d1=hole_d,d2=csink_d,h=csink_depth+0.02);");
            sb.AppendLine("  // Bovenste schroef ligt op de montagevleugel, volledig buiten de inschuifkamer.");
            sb.AppendLine("  translate([upper_hole_x,hole_spacing/2,total_d-csink_depth-0.01]) cylinder(d1=hole_d,d2=csink_d,h=csink_depth+0.02);");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildReadMe(WorkbenchCabinetConfig config, PlinthClipAdapterTemplate adapter)
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("SEKTION PLINTCLIP-ADAPTER V2 MET MONTAGEVLEUGEL");
            sb.AppendLine();
            sb.AppendLine("Ingemeten cliptong: " + adapter.TongueWidthMm.ToString("0.#", c) + " x " + adapter.TongueHeightMm.ToString("0.#", c) + " x " + adapter.TongueThicknessMm.ToString("0.#", c) + " mm.");
            sb.AppendLine("Printspeling: " + adapter.PrintClearancePerSideMm.ToString("0.##", c) + " mm per zijde.");
            sb.AppendLine("Korte adapter: " + ProductDefaults.WorkbenchCabinetFrontAdapterStandOffMm(config).ToString("0.#", c) + " mm uitstand, 2x verzonken-kopschroef 4x" + adapter.FrontScrewLengthMm.ToString("0", c) + ".");
            if (config.IncludeLeftSidePlinth || config.IncludeRightSidePlinth)
                sb.AppendLine("Zijadapter: " + ProductDefaults.WorkbenchCabinetSideAdapterStandOffMm(config).ToString("0.#", c) + " mm uitstand, 2x verzonken-kopschroef 4x" + adapter.SideScrewLengthMm.ToString("0", c) + ".");
            sb.AppendLine("Adaptergaten: doorvoer diameter " + adapter.MountingHoleDiameterMm.ToString("0.#", c)
                + " mm met conische kopzitting diameter " + adapter.MountingCountersinkDiameterMm.ToString("0.#", c)
                + " x " + adapter.MountingCountersinkDepthMm.ToString("0.#", c) + " mm.");
            sb.AppendLine("De onderste schroef ligt onder de inschuifkamer. De bovenste ligt "
                + adapter.UpperMountingHoleHorizontalOffsetMm.ToString("0.#", c)
                + " mm zijwaarts op een montagevleugel en blijft daardoor volledig buiten het schuifvlak.");
            sb.AppendLine("Gebruik bij de buitenste hoeken altijd de variant waarvan de vleugel naar het midden van de betreffende plint wijst.");
            sb.AppendLine("Houten plint: blinde pilotgaten diameter " + adapter.PlinthCenterMarkDiameterMm.ToString("0.#", c)
                + " x " + adapter.PlinthCenterMarkDepthMm.ToString("0.#", c) + " mm diep vanaf de binnenzijde.");
            sb.AppendLine();
            sb.AppendLine("Print vlak met de plintzijde op het bed, bij voorkeur PETG of ASA, 0,20mm laaghoogte, minimaal 4 perimeters en 40% infill.");
            sb.AppendLine("Print eerst één korte adapter. De cliptong moet van boven inschuiven zonder speling te rammelen. Pas zo nodig alleen de parameter clearance in het SCAD-bestand aan.");
            sb.AppendLine("Schroef handmatig aan de binnenzijde van de 18mm plint; niet door de zichtzijde. Maak de plint los door hem recht van de poten af te trekken.");
            return sb.ToString();
        }

        private static void Write(List<string> files, string folder, string root, string fileName, string content)
        {
            var path = Path.Combine(folder, fileName);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            files.Add(path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar));
        }
    }
}
