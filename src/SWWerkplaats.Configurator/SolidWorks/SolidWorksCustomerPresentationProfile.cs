using System;
using System.Collections.Generic;
using SWWerkplaats.Configurator.Domain;
using SWWerkplaats.Configurator.Portal;

namespace SWWerkplaats.Configurator.SolidWorks
{
    /// <summary>
    /// Productinhoud voor de herbruikbare klantbijlage. De PowerPoint-exporter
    /// beheert uitsluitend de vaste vormgeving; alle productspecifieke taal,
    /// camerastandpunten en technische accenten staan in dit profiel.
    /// </summary>
    internal sealed class SolidWorksCustomerPresentationProfile
    {
        public string ProductKey { get; set; }
        public string TemplateFileName { get; set; }
        public string DisplayName { get; set; }
        public string CoverSubtitle { get; set; }
        public string CoverPromise { get; set; }
        public string BenefitsEyebrow { get; set; }
        public string BenefitsTitle { get; set; }
        public string BenefitsIntroduction { get; set; }
        public IList<CustomerPresentationBenefit> Benefits { get; set; }
        public string SpecificationEyebrow { get; set; }
        public string SpecificationTitle { get; set; }
        public string SpecificationIntroduction { get; set; }
        public IList<CustomerPresentationSpecification> Specifications { get; set; }
        public string ScopeTitle { get; set; }
        public string ScopeBody { get; set; }
        public string ScopeApprovalText { get; set; }
        public string DrawingEyebrow { get; set; }
        public string DrawingTitle { get; set; }
        public string DrawingIntroduction { get; set; }
        public string DrawingNote { get; set; }
        public string DetailEyebrow { get; set; }
        public string DetailTitle { get; set; }
        public string DetailIntroduction { get; set; }
        public string DetailNote { get; set; }
        public string CoverImageAltText { get; set; }
        public string BenefitsImageAltText { get; set; }
        public double CoverRotationX { get; set; }
        public double CoverRotationY { get; set; }
        public double CoverRotationZ { get; set; }
        public double BenefitsRotationX { get; set; }
        public double BenefitsRotationY { get; set; }
        public double BenefitsRotationZ { get; set; }
        public bool ShowSlidingMovement { get; set; }
        public bool ShowHeightAdjustment { get; set; }
        public bool ShowBallTransferDetails { get; set; }
        public double HeightTravelMm { get; set; }
    }

    internal sealed class CustomerPresentationBenefit
    {
        public string Number { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
    }

    internal sealed class CustomerPresentationSpecification
    {
        public string Label { get; set; }
        public string Body { get; set; }
    }

    internal static class SolidWorksCustomerPresentationProfiles
    {
        private const string SharedTemplate = "Klantpresentatie-template.pptx";

        public static SolidWorksCustomerPresentationProfile Resolve(PortalQuoteRequest request, WorkbenchModel model)
        {
            var product = request == null ? "" : (request.Product ?? "").Trim();
            if (IsWorkstation(product)) return Workstation();
            return Generic(request, model);
        }

        private static SolidWorksCustomerPresentationProfile Workstation()
        {
            return new SolidWorksCustomerPresentationProfile
            {
                ProductKey = "workstation",
                TemplateFileName = SharedTemplate,
                DisplayName = "Workstation",
                CoverSubtitle = "Een werkstation dat werkhoogte, positionering en materiaalverplaatsing samenbrengt in één stabiele werkomgeving.",
                CoverPromise = "Comfortabel werken · gecontroleerd positioneren · soepel materiaal hanteren",
                BenefitsEyebrow = "WERKING EN VOORDELEN",
                BenefitsTitle = "Meer controle bij iedere handeling",
                BenefitsIntroduction = "Het werkstation ondersteunt de gebruiker bij het op werkhoogte brengen, verplaatsen en nauwkeurig positioneren van werkstukken.",
                Benefits = new[]
                {
                    Benefit("01", "Werk op passende hoogte", "Elektrische hoogteverstelling helpt de gebruiker een comfortabele werkpositie te kiezen."),
                    Benefit("02", "Verplaats met minder kracht", "Kogelpotten laten werkstukken soepel over het werkvlak bewegen."),
                    Benefit("03", "Positioneer beheerst", "Lineaire geleiding ondersteunt een nauwkeurige en gecontroleerde langsbeweging."),
                    Benefit("04", "Werk vanuit een stabiele basis", "De stijve profielconstructie geeft het werkvlak een robuuste ondersteuning.")
                },
                SpecificationEyebrow = "UITVOERING",
                SpecificationTitle = "Gebouwd voor dagelijks gebruik",
                SpecificationIntroduction = "De gekozen uitvoering combineert een slijtvast werkvlak met een stijve, onderhoudsarme constructie.",
                Specifications = new[]
                {
                    Specification("WERKVLAK", "Wit HPL met 53 verzonken RVS kogelpotten. De kogels steken 2 mm boven het werkvlak voor soepele materiaalverplaatsing."),
                    Specification("FRAME", "Mat geanodiseerde aluminium systeemprofielen vormen een stabiele en verzorgde draagconstructie."),
                    Specification("HOOGTEVERSTELLING", "Maximale werkbelasting: 2000 N. Digitaal hoogtedisplay met drie programmeerbare hoogte-instellingen. Soft-start, soft-stop en overbelastingsbeveiliging."),
                    Specification("GELEIDING", "Een lineair geleid werkvlak ondersteunt nauwkeurig positioneren in de langsrichting.")
                },
                ScopeTitle = "In deze uitvoering",
                ScopeBody = "Complete werkstationconstructie met werkvlak, onderstel, elektrische hoogteverstelling, lineaire geleiding, kogelpotten en montagecomponenten.",
                ScopeApprovalText = "Na akkoord worden de definitieve productiedetails en productietekeningen vrijgegeven.",
                DrawingEyebrow = "MAATCONTROLE",
                DrawingTitle = "Beweging en hoofdafmetingen in één overzicht",
                DrawingIntroduction = "De stippellijnen tonen de uiterste blad- en hoogteposities; de maatwaarden zijn de klantmaten voor akkoord.",
                DrawingNote = "Concept bij offerte. Definitieve productietekeningen en detailmaatvoering volgen na opdrachtbevestiging.",
                DetailEyebrow = "TECHNISCH DETAIL",
                DetailTitle = "Kogelpotpatroon en verzonken montage",
                DetailIntroduction = "Hartmaten, verspringing, randafstanden en inbouwhoogte zijn apart weergegeven voor een rustige maatcontrole.",
                DetailNote = "Definitieve productiedetails worden na opdrachtbevestiging vrijgegeven.",
                CoverImageAltText = "Rustig 3/4-bovenaanzicht van de geconfigureerde workstation-uitvoering",
                BenefitsImageAltText = "Rustig 3/4-zijaanzicht met werkvlak, onderstel en RVS-kogelpotpatroon",
                CoverRotationX = 36,
                CoverRotationY = -34,
                CoverRotationZ = 0,
                BenefitsRotationX = 42,
                BenefitsRotationY = 32,
                BenefitsRotationZ = 0,
                ShowSlidingMovement = true,
                ShowHeightAdjustment = true,
                ShowBallTransferDetails = true,
                HeightTravelMm = 400
            };
        }

        private static SolidWorksCustomerPresentationProfile Generic(PortalQuoteRequest request, WorkbenchModel model)
        {
            var name = FriendlyName(request, model);
            return new SolidWorksCustomerPresentationProfile
            {
                ProductKey = "generic",
                TemplateFileName = SharedTemplate,
                DisplayName = name,
                CoverSubtitle = "Een klantgerichte uitvoering waarin functie, materiaalkeuze en constructie samenkomen.",
                CoverPromise = "Functioneel ontworpen · degelijk uitgevoerd · afgestemd op de toepassing",
                BenefitsEyebrow = "WERKING EN VOORDELEN",
                BenefitsTitle = "Ontworpen voor de gewenste toepassing",
                BenefitsIntroduction = "Deze uitvoering is samengesteld vanuit de gekozen afmetingen, materialen en functionele eisen.",
                Benefits = new[]
                {
                    Benefit("01", "Afgestemd op de toepassing", "De maatvoering en uitvoering sluiten aan op de opgegeven gebruikssituatie."),
                    Benefit("02", "Duidelijke materiaalkeuze", "De toegepaste materialen worden overzichtelijk en controleerbaar vastgelegd."),
                    Benefit("03", "Stabiele constructie", "De constructieve opbouw ondersteunt betrouwbaar dagelijks gebruik."),
                    Benefit("04", "Controle vóór productie", "De hoofdafmetingen worden vóór vrijgave nog één keer met de klant gecontroleerd.")
                },
                SpecificationEyebrow = "UITVOERING",
                SpecificationTitle = "Samengesteld volgens configuratie",
                SpecificationIntroduction = "De klantbijlage beschrijft de gekozen uitvoering in begrijpelijke, merkneutrale taal.",
                Specifications = new[]
                {
                    Specification("MAATWERK", "Afmetingen en indeling volgens de gekozen configuratie."),
                    Specification("MATERIALEN", "Plaat- en profielmaterialen volgens de vastgelegde materiaalkeuze."),
                    Specification("CONSTRUCTIE", "Complete draagconstructie met de benodigde montagecomponenten."),
                    Specification("AFWERKING", "Verzorgde uitvoering passend bij de gekozen toepassing.")
                },
                ScopeTitle = "In deze uitvoering",
                ScopeBody = "De complete geconfigureerde constructie met de in deze klantbijlage beschreven onderdelen en materialen.",
                ScopeApprovalText = "Na akkoord worden de definitieve productiedetails en productietekeningen vrijgegeven.",
                DrawingEyebrow = "MAATCONTROLE",
                DrawingTitle = "Hoofdafmetingen in één overzicht",
                DrawingIntroduction = "De maatwaarden zijn de belangrijkste klantmaten voor controle en akkoord.",
                DrawingNote = "Concept bij offerte. Definitieve productietekeningen en detailmaatvoering volgen na opdrachtbevestiging.",
                DetailEyebrow = "TECHNISCH DETAIL",
                DetailTitle = "Aanvullende productdetails",
                DetailIntroduction = "Productspecifieke details worden in een apart, rustig overzicht weergegeven.",
                DetailNote = "Definitieve productiedetails worden na opdrachtbevestiging vrijgegeven.",
                CoverImageAltText = "3/4-bovenaanzicht van de geconfigureerde klantuitvoering",
                BenefitsImageAltText = "Aanvullend aanzicht van de geconfigureerde klantuitvoering",
                CoverRotationX = 36,
                CoverRotationY = -34,
                CoverRotationZ = 0,
                BenefitsRotationX = 58,
                BenefitsRotationY = -25,
                BenefitsRotationZ = 0,
                ShowSlidingMovement = false,
                ShowHeightAdjustment = false,
                ShowBallTransferDetails = false,
                HeightTravelMm = 0
            };
        }

        private static CustomerPresentationBenefit Benefit(string number, string title, string body)
        {
            return new CustomerPresentationBenefit { Number = number, Title = title, Body = body };
        }

        private static CustomerPresentationSpecification Specification(string label, string body)
        {
            return new CustomerPresentationSpecification { Label = label, Body = body };
        }

        private static bool IsWorkstation(string product)
        {
            return string.Equals(product, "werktafel_lex", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product, "werktafel_lex_revolution", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product, "lex-werktafel", StringComparison.OrdinalIgnoreCase);
        }

        private static string FriendlyName(PortalQuoteRequest request, WorkbenchModel model)
        {
            var project = request == null ? "" : (request.ProjectName ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(project)) return project;
            project = model == null ? "" : (model.ProjectName ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(project)) return project;
            var product = request == null ? "" : (request.Product ?? "").Trim().Replace('_', ' ');
            return string.IsNullOrWhiteSpace(product) ? "Klantuitvoering" : product;
        }
    }
}
