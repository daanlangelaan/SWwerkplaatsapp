namespace SWWerkplaats.Configurator.Portal
{
    public static class PortalHtml
    {
        public static string Page()
        {
            return @"<!doctype html>
<html lang=""nl"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>SW Werkplaats Portal</title>
  <style>
    :root{--bg:#f5f5f7;--panel:rgba(255,255,255,.86);--panel2:#fff;--ink:#1d1d1f;--muted:#6e6e73;--line:#d8d8de;--soft:#eef0f4;--accent:#0071e3;--accent2:#1d7f5f;--warn:#bf5b00;--danger:#b42318;--shadow:0 18px 55px rgba(20,24,33,.10)}
    *{box-sizing:border-box}html{background:var(--bg)}body{margin:0;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;background:radial-gradient(circle at 20% -10%,#ffffff 0,#f5f5f7 36%,#eef1f5 100%);color:var(--ink);letter-spacing:0}
    header{height:72px;display:flex;align-items:center;justify-content:space-between;padding:0 34px;border-bottom:1px solid rgba(0,0,0,.06);background:rgba(255,255,255,.72);backdrop-filter:blur(18px);position:sticky;top:0;z-index:5}
    h1{font-size:23px;margin:0;font-weight:760}h2{font-size:20px;margin:0 0 14px;font-weight:760}h3{font-size:13px;margin:0 0 10px;color:var(--muted);font-weight:720;text-transform:uppercase;letter-spacing:.04em}
    .brand{display:flex;align-items:center;gap:12px}.mark{width:34px;height:34px;border-radius:10px;background:linear-gradient(145deg,#111827,#485465);box-shadow:inset 0 1px 0 rgba(255,255,255,.22)}.headerTools{display:flex;align-items:center;gap:12px}.topMeta{font-size:13px;color:var(--muted)}.stopPortal{padding:8px 10px;border-radius:10px;background:#eef0f4;color:#344054;box-shadow:none;font-size:12px}
    .start{min-height:calc(100vh - 72px);display:grid;align-content:center;gap:28px;padding:42px;max-width:1120px;margin:0 auto}.hero{text-align:center}.hero h2{font-size:52px;line-height:1.02;margin:0 auto 12px;max-width:760px}.hero p{font-size:19px;color:var(--muted);margin:0 auto;max-width:670px}
    .choices{display:grid;grid-template-columns:1fr 1fr;gap:22px}.choice{border:1px solid rgba(0,0,0,.08);border-radius:18px;background:rgba(255,255,255,.9);box-shadow:0 18px 55px rgba(20,24,33,.10);padding:20px;cursor:pointer;text-align:left;transition:transform .18s ease,box-shadow .18s ease,border-color .18s ease}.choice:hover{transform:translateY(-3px);border-color:rgba(0,113,227,.28);box-shadow:0 24px 70px rgba(20,24,33,.14)}
    .choiceArt{height:250px;border-radius:14px;background:#fff;margin-bottom:14px;display:grid;place-items:center;overflow:hidden;border:1px solid rgba(0,0,0,.06)}.choiceArt img{width:100%;height:100%;object-fit:contain;display:block}.choiceImageLabel{text-align:center;font-size:15px;font-weight:760;margin:8px 0 18px;color:#1d1d1f}
    .choice h3{font-size:24px;text-transform:none;letter-spacing:0;color:var(--ink);margin-bottom:7px}.choice p{color:var(--muted);font-size:15px;line-height:1.5;margin:0 0 18px}.choice span{color:var(--accent);font-weight:760}
    main{display:none;grid-template-columns:minmax(330px,390px) minmax(520px,1fr);gap:22px;padding:24px;min-height:calc(100vh - 72px);max-width:1480px;margin:0 auto}.appOn main{display:grid}.appOn .start{display:none}
    .stack{display:grid;gap:18px}.panel{background:var(--panel);border:1px solid rgba(0,0,0,.08);border-radius:24px;box-shadow:0 10px 35px rgba(20,24,33,.07);padding:20px;min-width:0}.glass{backdrop-filter:blur(18px)}
    label{display:block;font-size:12px;color:var(--muted);margin:12px 0 6px;font-weight:650}input,select,textarea{width:100%;border:1px solid transparent;border-radius:13px;padding:12px 13px;font:inherit;background:#f5f6f8;color:var(--ink);outline:none;transition:border-color .15s ease,background .15s ease,box-shadow .15s ease}input:focus,select:focus,textarea:focus{background:#fff;border-color:rgba(0,113,227,.35);box-shadow:0 0 0 4px rgba(0,113,227,.12)}textarea{min-height:74px;resize:vertical}.row{display:grid;grid-template-columns:1fr 1fr;gap:12px}
    .checks{display:grid;gap:6px;margin-top:10px}.checks label{display:flex;gap:9px;align-items:center;color:var(--ink);font-size:14px;margin:0}.checks input{width:auto;accent-color:var(--accent)}
    .productLock{border:1px solid transparent;border-radius:13px;padding:12px 13px;background:#f5f6f8;color:var(--ink);font-size:15px;font-weight:650}
    button{border:0;border-radius:14px;padding:12px 15px;font-weight:760;cursor:pointer;background:var(--accent);color:#fff;box-shadow:0 7px 18px rgba(0,113,227,.20)}button.secondary{background:#2f3641;box-shadow:none}button.warn{background:var(--warn);box-shadow:0 7px 18px rgba(191,91,0,.18)}button.ghost{background:#eef0f4;color:#1d1d1f;box-shadow:none}button:disabled{opacity:.52;cursor:not-allowed}.toolbar{display:flex;gap:10px;flex-wrap:wrap;margin-top:16px}
    .generateBar{position:sticky;bottom:16px;margin-top:18px;padding:12px;border-radius:20px;background:rgba(255,255,255,.90);border:1px solid rgba(0,0,0,.08);box-shadow:0 14px 38px rgba(20,24,33,.12);backdrop-filter:blur(18px)}.generateActions{display:grid;grid-template-columns:1fr 1fr;gap:10px}.generateBar button{width:100%;font-size:16px;padding:14px 16px}.exportOptions{margin-top:10px;padding:10px 11px;border-radius:14px;background:#f5f6f8}.exportOptionsTitle{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-bottom:7px;color:var(--muted);font-size:11px;font-weight:760}.exportOptionGrid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:7px}.exportOptionGrid label{display:flex;align-items:center;gap:6px;margin:0;padding:7px 8px;border-radius:10px;background:#fff;color:var(--ink);font-size:11px;font-weight:680}.exportOptionGrid label:last-child{grid-column:span 2}.exportOptionGrid input{width:auto;margin:0;accent-color:var(--accent)}.exportHint{font-size:11px;color:var(--muted);text-align:center;margin-top:7px}.dirtyNote{font-size:12px;color:var(--muted);text-align:center;margin-top:8px}
    .calcNote{font-size:12px;color:var(--muted);margin-top:8px}
    .pricePanel{display:grid;grid-template-columns:1fr auto;gap:12px;align-items:start}.price{font-size:42px;font-weight:820;margin:4px 0 6px;line-height:1}.priceBreakdown{display:grid;grid-template-columns:repeat(6,minmax(105px,1fr));gap:8px;margin:10px 0 6px;max-width:1040px}.priceBreakdown span{display:block;border-radius:12px;background:#f5f6f8;padding:8px 10px;font-size:12px;color:var(--muted)}.priceBreakdown strong{display:block;color:var(--ink);font-size:15px;margin-top:2px}.muted{color:var(--muted)}.error{color:var(--danger);font-weight:760}.summaryLine{font-size:15px;color:var(--muted)}.lead{font-size:14px;color:var(--muted)}
    .previewGrid{display:grid;grid-template-columns:1.45fr .9fr;gap:18px}.sidePreviews{display:grid;gap:18px}.svgbox,.canvasbox,.orthobox{border:1px solid rgba(0,0,0,.08);border-radius:20px;background:linear-gradient(180deg,#fff,#f5f7fa);overflow:hidden;min-height:260px}.svgbox svg{width:100%;height:auto;display:block}.canvasbox{position:relative;min-height:520px}.canvasbox canvas{position:absolute;inset:0;width:100%;height:100%;min-height:520px;display:block;cursor:grab}.orthobox{position:relative;min-height:330px}.orthobox canvas{width:100%;height:100%;min-height:330px;display:block}.canvasbox canvas:active{cursor:grabbing}.webglOn #assemblyFallbackCanvas{display:none}.webglOn #assemblyCanvas{display:block}#assemblyCanvas{display:none}.viewerHint{position:absolute;z-index:3;left:18px;bottom:18px;right:18px;display:flex;gap:10px;align-items:center;padding:10px 12px;border-radius:16px;background:rgba(255,255,255,.84);backdrop-filter:blur(14px);box-shadow:0 8px 24px rgba(20,24,33,.10);font-size:13px;color:var(--muted)}.viewerHint input{padding:0;background:transparent;box-shadow:none;border:0;accent-color:var(--accent)}.sectionHead{display:flex;align-items:center;justify-content:space-between;margin-bottom:10px}.badge{display:inline-flex;align-items:center;border-radius:999px;padding:6px 10px;background:#eef7f3;color:#1d7f5f;font-size:12px;font-weight:760}.viewActions{display:flex;align-items:center;gap:10px}.viewActions button{padding:8px 11px;border-radius:11px;font-size:12px;background:#eef0f4;color:#1d1d1f;box-shadow:none}.lexViewerTools{display:none;margin:0 0 12px;padding:10px 12px;border-radius:14px;background:#f3f5f8;align-items:center;justify-content:space-between;gap:12px;flex-wrap:wrap}.isLex .lexViewerTools{display:flex}.lexToolGroups,.lexViewButtons,.lexLegend,.modalHeadActions{display:flex;align-items:center;gap:7px;flex-wrap:wrap}.lexToolGroups{gap:14px}.lexViewerTools strong{font-size:12px;color:#667085;margin-right:3px}.lexViewButtons button{padding:7px 10px;border-radius:9px;background:#fff;color:#344054;box-shadow:inset 0 0 0 1px rgba(52,64,84,.12);font-size:12px}.lexViewButtons button.active{background:#1f4b73;color:#fff;box-shadow:none}.lexLegend span{font-size:11px;color:#667085;white-space:nowrap}.legendDot{display:inline-block;width:9px;height:9px;border-radius:3px;margin-right:4px;vertical-align:-1px}.legendProfile{background:#6e88a2}.legendLift{background:#1f4b73}.legendGuide{background:#f28c28}.legendSheet{background:#d6a348}.realAssemblyColors .legendProfile{background:#b7bec4}.realAssemblyColors .legendLift{background:#6f777e}.realAssemblyColors .legendGuide{background:#747d85}.realAssemblyColors .legendSheet{background:#f2f1ec;box-shadow:inset 0 0 0 1px #c9c9c5}.isLex .previewGrid{grid-template-columns:1fr}.isLex .sidePreviews{grid-template-columns:1fr 1fr}.isLex .canvasbox,.isLex .canvasbox canvas{min-height:610px}.isLex .orthobox,.isLex .orthobox canvas{min-height:390px}.modal{display:none;position:fixed;inset:0;z-index:30;background:rgba(245,245,247,.72);backdrop-filter:blur(18px);padding:28px}.modalOn .modal{display:grid}.modalPanel{background:#fff;border-radius:26px;box-shadow:0 24px 80px rgba(20,24,33,.22);display:grid;grid-template-rows:auto 1fr;min-height:0;max-height:calc(100vh - 56px);overflow:hidden}.modalHead{display:flex;align-items:center;justify-content:space-between;padding:16px 18px;border-bottom:1px solid rgba(0,0,0,.08)}.modalHeadActions{justify-content:flex-end}.modalAssemblyTools{display:none;align-items:center;gap:7px;flex-wrap:wrap}.modalAssemblyTools button{padding:7px 10px;border-radius:9px;background:#eef0f4;color:#344054;box-shadow:none;font-size:12px}.modalAssemblyTools button.active{background:#1f4b73;color:#fff}.modalToolDivider{width:1px;height:22px;background:rgba(52,64,84,.16);margin:0 3px}.modalBody{padding:18px;min-height:0;overflow:auto}.modalBody .canvasbox,.modalBody .orthobox{height:calc(100vh - 150px);min-height:520px}.modalBody .svgbox{max-height:none;overflow:visible}.modalBody .svgbox svg{max-width:none}.modalBody .nestingZoomHost{height:calc(100vh - 150px);overflow:auto;cursor:zoom-in}.modalBody .nestingZoomHost svg{max-width:none}.modalBody canvas{min-height:100%}
    table{width:100%;border-collapse:collapse;font-size:13px}th,td{text-align:left;border-bottom:1px solid var(--line);padding:10px 6px;vertical-align:top}th{color:var(--muted);font-weight:760}.pill{display:inline-block;border-radius:999px;padding:5px 9px;background:#eef0f4;color:#344054;font-weight:720;font-size:12px}.orderTools button{padding:8px 10px;border-radius:10px}
    .productOnlyWorkbench,.productOnlyCubby,.productOnlyWorkbenchCabinet,.lexFixedMaterials{display:none}.isWorkbench .productOnlyCabinet,.isWorkbench .productOnlyCabinetOrCubby{display:none}.isWorkbench .productOnlyWorkbench{display:block}.isWorkbench .unitField,.isCubby .unitField{display:none}.isLex .workbenchShelfOptions,.isLex #primarySheetMaterialField,.isLex #profileMaterialField{display:none}.isLex .lexFixedMaterials{display:block}.lexFixedMaterials{margin:14px 0 4px;padding:13px 14px;border-radius:14px;background:#eef7f3;color:#285f50;font-size:13px;line-height:1.5}.lexFixedMaterials strong{display:block;color:#1d1d1f;margin-bottom:3px}.isCubby .productOnlyCabinet{display:none}.isCubby .productOnlyCubby{display:block}.isCubby .cabinetOnly{display:none}.isCubby .productOnlyCabinetOrCubby{grid-template-columns:1fr}.isWorkbenchCabinet .productOnlyCabinet,.isWorkbenchCabinet .cabinetOnly{display:none}.isWorkbenchCabinet .productOnlyCabinetShelves{display:grid}.isWorkbenchCabinet .adjustableShelfOption,.isWorkbenchCabinet .workbenchCabinetDrawerOption{display:flex}.isWorkbenchCabinet .drawerMaterialOption{display:block}.isWorkbenchCabinet .productOnlyWorkbenchCabinet{display:block}.isWorkbenchCabinet .productOnlyCabinetOrCubby{grid-template-columns:1fr}.isCubby #widthMm,.isCubby #depthMm,.isCubby #heightMm{background:#eef7f3;color:#1d7f5f;font-weight:760}#slidingDoorOptions{display:none}.slidingDoorMode #slidingDoorOptions{display:block}
    .productOnlyMachineBase,.productOnlyRobotCell,.productOnlyShippingBox{display:none}.isMachineBase .productOnlyMachineBase,.isRobotCell .productOnlyRobotCell,.isShippingBox .productOnlyShippingBox{display:block}.isMachineBase .productOnlyCabinet,.isMachineBase .productOnlyCabinetOrCubby,.isMachineBase #primarySheetMaterialField,.isMachineBase .unitField,.isRobotCell .productOnlyCabinet,.isRobotCell .productOnlyCabinetOrCubby,.isRobotCell #primarySheetMaterialField,.isRobotCell .unitField,.isShippingBox .productOnlyCabinet,.isShippingBox .productOnlyCabinetOrCubby,.isShippingBox .productOnlyWorkbench,.isShippingBox .productOnlyWorkbenchCabinet,.isShippingBox .unitField{display:none}.shippingSourceNote{margin:10px 0 4px;padding:13px 14px;border-radius:14px;background:#fff7e8;color:#7a4b00;font-size:13px;line-height:1.5}.shippingSourceNote strong{display:block;color:#1d1d1f;margin-bottom:4px}
    @media(max-width:1180px){main{grid-template-columns:1fr}.previewGrid{grid-template-columns:1fr}.choices{grid-template-columns:1fr}.hero h2{font-size:38px}}@media(max-width:820px){.isLex .sidePreviews{grid-template-columns:1fr}.exportOptionGrid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:680px){header{padding:0 18px}.topMeta{display:none}.start{padding:24px}.hero h2{font-size:34px}.row,.generateActions{grid-template-columns:1fr}.pricePanel{grid-template-columns:1fr}.price{font-size:34px}.lexLegend{display:none}}
  </style>
</head>
<body>
  <header>
    <div class=""brand""><div class=""mark""></div><h1>SW Werkplaats Portal</h1></div>
    <div class=""headerTools""><div class=""topMeta"">Configuratie, prijs en freeswachtrij</div><button class=""stopPortal"" type=""button"" onclick=""location.href='/library'"">Bibliotheek</button><button class=""stopPortal"" type=""button"" onclick=""stopPortal()"">Stop portal</button></div>
  </header>

  <section class=""start"" id=""start"">
    <div class=""hero"">
      <h2>Kies wat je wilt configureren.</h2>
      <p>Begin met een machinebasis, werktafel, kastonderbouw, cabinet of vakjeskast. Daarna zie je direct een prijsindicatie, nette visualisatie en de interne werkplaatsflow.</p>
    </div>
    <div class=""choices"">
      <button class=""choice"" type=""button"" onclick=""chooseProduct('werktafel')"">
        <div class=""choiceArt"">
          <img src=""/images/product-workbench.png"" alt=""Voorbeeld werktafel"">
        </div>
        <div class=""choiceImageLabel"">Werktafel / werkbank</div>
        <h3>Werktafel</h3>
        <p>Frame, blad, freesbare plaatdelen en werkplaatsoutput voor een parametrische tafel.</p>
        <span>Configureer werktafel</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('machinebasis')"">
        <div class=""choiceArt""><img src=""/images/product-workbench.png"" alt=""Parametrisch aluminium machineframe""></div>
        <div class=""choiceImageLabel"">Machinebasis / modulair frame</div>
        <h3>Parametrische machinebasis</h3>
        <p>Doorlopende 40x80-staanders, drie liggerlagen, voetplaten en nivellerende zwenkwielen.</p>
        <span>Configureer machinebasis</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('robotcel')"">
        <div class=""choiceArt""><img src=""/images/product-workbench.png"" alt=""Robotcel uit aluminium systeemprofielen""></div>
        <div class=""choiceImageLabel"">Robot cel / modulair frame</div>
        <h3>Robot cel</h3>
        <p>M16-stelvoeten, 80x80-staanders, een staand 40x80-onderframe met één dwarsligger en een staand 40x80-bladframe op geldige T-slotbanen.</p>
        <span>Configureer robot cel</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('werktafel_lex')"">
        <div class=""choiceArt"">
          <img src=""/images/product-workbench.png"" alt=""Workstation met hoogteverstelling"">
        </div>
        <div class=""choiceImageLabel"">Workstation / in hoogte verstelbaar</div>
        <h3>Workstation</h3>
        <p>Elektrische hoogteverstelling, lineaire geleiding, schuifbaar kogelpotblad en complete profielconstructie.</p>
        <span>Open workstation</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('werktafel_lex_revolution')"">
        <div class=""choiceArt"">
          <img src=""/images/product-workbench.png"" alt=""Ontwikkelvariant workstation"">
        </div>
        <div class=""choiceImageLabel"">Workstation / ontwikkelvariant</div>
        <h3>Workstation ontwikkelvariant</h3>
        <p>Zelfstandige doorontwikkeling van de offerbare basis, bedoeld voor nieuwe constructie-oplossingen.</p>
        <span>Open Revolution-ontwerp</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('cabinet')"">
        <div class=""choiceArt"">
          <img src=""/images/product-cabinet.png"" alt=""Voorbeeld cabinet"">
        </div>
        <div class=""choiceImageLabel"">Cabinet / kast</div>
        <h3>Cabinet / kast</h3>
        <p>Units, lades, deuren, legplanken, nesting en ordervrijgave voor productie.</p>
        <span>Configureer cabinet</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('werkbankkast')"">
        <div class=""choiceArt"">
          <img src=""/images/product-cabinet.png"" alt=""Voorbeeld werkbank met kastonderbouw"">
        </div>
        <div class=""choiceImageLabel"">Werkbank met kastonderbouw</div>
        <h3>Doorlopende bodem</h3>
        <p>Deurparen met T-stijlen, gaten voor stelpoten, één bodemplaat en een losse voorzetplint.</p>
        <span>Configureer kastonderbouw</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('vakjeskast')"">
        <div class=""choiceArt"">
          <img src=""/images/product-cubby.png"" alt=""Voorbeeld vakjeskast"">
        </div>
        <div class=""choiceImageLabel"">Vakjeskast</div>
        <h3>Vakjeskast</h3>
        <p>Grid met kamdelen, achterwandsegmenten, positioneergroeven en nesting per plaatmateriaal.</p>
        <span>Configureer vakjeskast</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('shipping_box')"">
        <div class=""choiceArt"">
          <img src=""/images/product-cabinet.png"" alt=""Parametrische houten shipping box met clips"">
        </div>
        <div class=""choiceImageLabel"">Shipping box / demontabele clipkist</div>
        <h3>Shipping box</h3>
        <p>Binnenmaten leidend, plaatmateriaal en dikte kiesbaar, CNC-sponningen en clipsleuven met optionele handgrepen.</p>
        <span>Configureer shipping box</span>
      </button>
    </div>
  </section>

  <main id=""app"">
    <div class=""stack"">
      <section class=""panel glass"" id=""configPanel"">
        <div class=""sectionHead""><h2>Klantconfigurator</h2><button class=""ghost"" type=""button"" onclick=""backToStart()"">Terug naar productkeuze</button></div>
        <label>Product</label>
        <input id=""product"" type=""hidden"" value=""cabinet"">
        <div class=""productLock"" id=""productName"">Cabinet / kast</div>
        <div class=""row"">
          <div><label id=""widthLabel"">Breedte mm</label><input id=""widthMm"" type=""number"" value=""2400""></div>
          <div><label id=""depthLabel"">Diepte mm</label><input id=""depthMm"" type=""number"" value=""600""></div>
        </div>
        <div class=""row"">
          <div><label id=""heightLabel"">Hoogte mm</label><input id=""heightMm"" type=""number"" value=""900""></div>
          <div><label>Aantal stuks</label><input id=""quantity"" type=""number"" value=""1"" min=""1"" max=""99""></div>
        </div>
        <div class=""row"">
          <div class=""unitField""><label>Units</label><input id=""unitCount"" type=""number"" value=""4"" min=""1"" max=""12""></div>
        </div>
        <div class=""productOnlyCubby"">
          <h3>Vakjes parameters</h3>
          <div class=""row"">
            <div><label>Vak breedte mm</label><input id=""cubbyCellWidthMm"" type=""number"" value=""400"" min=""40""></div>
            <div><label>Vak diepte mm</label><input id=""cubbyCellDepthMm"" type=""number"" value=""350"" min=""40""></div>
          </div>
          <div class=""row"">
            <div><label>Vak hoogte mm</label><input id=""cubbyCellHeightMm"" type=""number"" value=""350"" min=""40""></div>
            <div><label>Vakken breedte</label><input id=""cubbyColumnCount"" type=""number"" value=""3"" min=""1"" max=""12""></div>
          </div>
          <div class=""row"">
            <div><label>Vakken hoogte</label><input id=""cubbyRowCount"" type=""number"" value=""4"" min=""1"" max=""12""></div>
            <div><label>Vakjes verdiept mm</label><input id=""cubbyGridInsetMm"" type=""number"" value=""20"" min=""0""></div>
          </div>
          <div id=""cubbyCombCount"" class=""calcNote""></div>
        </div>
        <div class=""productOnlyMachineBase"">
          <h3>Machineframe fase 1</h3>
          <div class=""row"">
            <div><label>Bladhoogte (bovenzijde) mm</label><input id=""machineBaseWorktopHeightMm"" type=""number"" value=""900"" min=""600"" max=""1000"" step=""10""></div>
            <div><label>Werkblad</label><select id=""machineBaseWorktopMaterialId""><option value=""hpl_10_lex"">HPL wit 10 mm</option><option value=""hpl_12_machinebase"">HPL wit 12 mm</option></select></div>
          </div>
          <div><label>Staanders</label><div class=""productLock"">40x80 · 80 mm in diepte · werkblad met vier 41x81 mm hoekuitsparingen</div></div>
          <div class=""row"">
            <div><label>Onderliggers</label><select id=""machineBaseLowerBeamProfileId""><option value=""alu_system_40x40"">40x40</option><option value=""alu_system_80x40"">40x80 · 80 verticaal</option></select></div>
            <div><label>Bladliggers buitencontour</label><input id=""machineBaseWorktopBeamProfileId"" type=""hidden"" value=""alu_system_80x40""><div class=""productLock"">Altijd 40x80 · 80 mm verticaal</div></div>
          </div>
          <div><label>Maximale h.o.h.-afstand tussenliggers mm</label><input id=""machineBaseWorktopIntermediateBeamMaxSpacingMm"" type=""number"" value=""500"" min=""300"" max=""1000"" step=""10""></div>
          <h3>Besturingskast en voorbeveiliging</h3>
          <div class=""row""><div><label>Voorzijde boven werkblad</label><select id=""machineBaseFrontProtectionMode""><option value=""doors"">Twee profieldeuren</option><option value=""lightcurtain"">Lichtgordijn</option></select></div><div><label>Aantal profieldeuren</label><select id=""machineBaseFrontDoorCount""><option value=""2"">2</option><option value=""1"">1</option></select></div></div>
          <div class=""row""><div><label>Scharnierzijde enkel profieldeur</label><select id=""machineBaseFrontSingleDoorHingeSide""><option value=""left"">Links</option><option value=""right"">Rechts</option></select></div><div><label>Besturingskast deuren</label><select id=""machineBaseControlCabinetDoorCount""><option value=""2"">2</option><option value=""1"">1</option></select></div></div>
          <div class=""row""><div><label>Kast breedte mm</label><input id=""machineBaseControlCabinetWidthMm"" type=""number"" value=""800"" min=""300"" step=""10""></div><div><label>Kast diepte mm</label><input id=""machineBaseControlCabinetDepthMm"" type=""number"" value=""400"" min=""200"" step=""10""></div></div>
          <div class=""row""><div><label>Kast hoogte mm</label><input id=""machineBaseControlCabinetHeightMm"" type=""number"" value=""600"" min=""300"" step=""10""></div><div><label>Positie besturingskast</label><select id=""machineBaseControlCabinetPosition""><option value=""left"">Kast links</option><option value=""right"">Kast rechts</option></select></div></div>
          <div><label>Scharnierzijde enkele kastdeur</label><select id=""machineBaseControlCabinetHingeSide""><option value=""left"">Links</option><option value=""right"">Rechts</option></select></div>
          <div class=""calcNote"">De tussenliggers gebruiken hetzelfde profiel als de werkblad-buitencontour, lopen in de diepte en worden altijd opnieuw gelijkmatig over de breedte verdeeld. Alle hoogten worden vanaf de vloer gemeten in bedrijfsstand: de GD-60S staat 10 mm uit op zijn rubberen stelvoet en het transportwiel is vrij.</div>
        </div>
        <div class=""productOnlyRobotCell"">
          <h3>Robotcel frame</h3>
          <div class=""productLock"">M16-stelvoeten → 80x80/M16-voetplaten → 80x80-staanders → staand 40x80-onderframe met exact 1 dwarsligger → staand 40x80-bladframe + dwarsliggers op geldige T-slotbanen → HPL-blad → groef-8 40x160-achterrail met 2 zwarte eindkappen</div>
          <div><label>Maximale h.o.h.-afstand dwarsliggers mm</label><input id=""robotCellIntermediateBeamMaxSpacingMm"" type=""number"" value=""500"" min=""300"" max=""1000"" step=""10""></div>
          <div class=""calcNote"">Profielen en profieltoebehoren gebruiken de centrale leveranciersvolgorde. TechXXL is standaard rang 1 voor alle bestaande en nieuwe producten; alternatieve leveranciers kunnen later met rang 2 of hoger worden toegevoegd.</div>
        </div>
        <div class=""productOnlyShippingBox"">
          <h3>Clipkist parameters</h3>
          <div><label>Montageverbinding</label><select id=""shippingBoxJointMode""><option value=""rabbet"">Doorlopende sponning</option><option value=""localized_tabs"">Zelfschalende montagetappen bij clips</option></select></div>
          <div class=""checks""><label><input id=""shippingBoxIncludeHandles"" type=""checkbox""> Uitgefreesde handgrepen in beide zijpanelen</label></div>
          <div class=""shippingSourceNote""><strong>Leverancier en proefstukstatus</strong>Liangyue LY103-12 veerclip. De clipvorm is uit leveranciersfoto’s afgeleid; sleuf 32 × 8 mm en 32 mm hartafstand tot de rand moeten na ontvangst van een sample worden ingemeten.</div>
          <div class=""calcNote"" id=""shippingBoxOuterDimensions"">Buitenmaten worden berekend uit de binnenmaten en gekozen plaatdikte.</div>
        </div>
        <div id=""primarySheetMaterialField""><label>Plaatmateriaal</label><select id=""sheetMaterialId""></select></div>
        <div class=""row productOnlyCabinetOrCubby"">
          <div class=""cabinetOnly drawerMaterialOption""><label>Plaatmateriaal lades</label><select id=""drawerMaterialId""></select></div>
          <div><label>Plaatmateriaal achterwand</label><select id=""backMaterialId""></select></div>
        </div>
        <div class=""productOnlyWorkbench"">
          <div id=""profileMaterialField""><label>Profielmateriaal</label><select id=""profileMaterialId""></select></div>
          <div class=""lexFixedMaterials""><strong>Vaste workstation-uitvoering</strong>Wit HPL 10 mm werkblad · wit HPL 6 mm stabilisatieplaat · geanodiseerde aluminium systeemprofielen · aluminium adapterplaten · lineaire geleidingen · elektrische hefkolommen.</div>
          <label>Profielen afkorten</label>
          <select id=""profileSawingMode""><option value=""supplier"">Op maat inkopen bij leverancier</option><option value=""inhouse"">Zelf zagen in de werkplaats</option></select>
          <div class=""checks workbenchShelfOptions"">
            <label><input id=""includeLowerShelf"" type=""checkbox""> Onderblad met uitsparingen</label>
            <label><input id=""includeMiddleShelf"" type=""checkbox""> Ligblad / tussenblad met uitsparingen</label>
          </div>
          <div class=""row workbenchShelfOptions"">
            <div><label>Hoogte onderblad mm</label><input id=""lowerShelfHeightMm"" type=""number"" value=""180""></div>
            <div><label>Hoogte ligblad mm</label><input id=""middleShelfHeightMm"" type=""number"" value=""450""></div>
          </div>
        </div>
        <div class=""productOnlyWorkbenchCabinet"">
          <h3>Doorlopende kastonderbouw</h3>
          <div class=""row"">
            <div><label>Ingestelde poothoogte mm</label><input id=""workbenchCabinetPlinthHeightMm"" type=""number"" value=""114"" min=""88.9"" max=""130.2"" step=""0.1""></div>
            <div><label>Plint terugligging mm</label><input id=""workbenchCabinetPlinthSetbackMm"" type=""number"" value=""45"" min=""0""></div>
          </div>
          <div class=""row"">
            <div><label>Geselecteerde stelpoot</label><input value=""IKEA SEKTION 905.560.71"" readonly></div>
            <div><label>Inset hart zij-/achterpoten mm</label><input id=""workbenchCabinetFootInsetMm"" type=""number"" value=""55"" min=""27""></div>
          </div>
          <div class=""row"">
            <div><label>Hart poot vanaf achtervlak cliptong mm</label><input id=""workbenchCabinetPlinthClipCenterBehindBackFaceMm"" type=""number"" value=""25.4"" min=""5"" step=""0.1""></div>
            <div><label>Berekend hart voorpoten vanaf kastfront mm</label><input id=""workbenchCabinetCalculatedFrontFootInsetMm"" type=""number"" value=""91.4"" readonly></div>
          </div>
          <div class=""row"">
            <div><label>Berekend hart hoekpoten bij zijplint mm</label><input id=""workbenchCabinetCalculatedSideFootInsetMm"" type=""number"" value=""66"" readonly></div>
            <div><label>Vrije ruimte montagevoet tot plint mm</label><input value=""1"" readonly></div>
          </div>
          <div class=""row"">
            <div><label>Clip-inschuiftong mm</label><input value=""28 × 34,5 × 3,3"" readonly></div>
            <div><label>Printspeling per zijde mm</label><input value=""0.25"" readonly></div>
          </div>
          <div class=""row"">
            <div><label>Uitstand adapter voorplint mm</label><input id=""workbenchCabinetCalculatedFrontAdapterStandOffMm"" value=""3"" readonly></div>
            <div><label>Uitstand adapter zijplint mm</label><input id=""workbenchCabinetCalculatedSideAdapterStandOffMm"" value=""22.6"" readonly></div>
          </div>
          <div class=""checks"">
            <label><input id=""workbenchCabinetIncludeLeftSidePlinth"" type=""checkbox""> Zijplint links</label>
            <label><input id=""workbenchCabinetIncludeRightSidePlinth"" type=""checkbox""> Zijplint rechts</label>
            <label><input id=""enableWoodScrewCountersinks"" type=""checkbox""> Hout-op-hout schroefgaten verzinken — Ø8 × 5 mm</label>
            <label><input id=""enableOutsideEdgeChamfer"" type=""checkbox""> Volledige buitencontour afschuinen — 1 × 1 mm</label>
          </div>
          <div class=""note"">Beide proefbewerkingen blijven standaard uit en zijn onafhankelijk aanvinkbaar. V-frees: 90° Ø8, 2 snijders, rechtsdraaiend, 18.000 rpm, voeding 600 mm/min en plunge 150 mm/min.</div>
          <div class=""row"">
            <div><label>Breedte deuraanslag mm</label><input id=""workbenchCabinetDoorStopWidthMm"" type=""number"" value=""50"" min=""22""></div>
            <div><label>Offset legplanken vanaf voorzijde mm</label><input id=""workbenchCabinetShelfFrontInsetMm"" type=""number"" value=""0"" min=""0""></div>
          </div>
          <div class=""row"">
            <div><label>Hoogte bovenste ladefront mm</label><input id=""workbenchCabinetTopDrawerHeightMm"" type=""number"" value=""160"" min=""100"" max=""320""></div>
            <div><label>Verstelposities legplanken</label><input id=""workbenchCabinetShelfPositionCount"" type=""number"" value=""6"" min=""1"" max=""20""></div>
          </div>
          <div class=""row"">
            <div><label>Hoekradius deuren en ladefronten mm</label><input id=""workbenchCabinetFrontPanelCornerRadiusMm"" type=""number"" value=""2"" min=""0"" max=""25"" step=""0.5""></div>
            <div><label>Toepassing</label><input value=""Alle deuren en ladefronten"" readonly></div>
          </div>
          <div class=""calcNote"">SEKTION 11 cm: montagevoet 76×51×12mm, twee gemeten klikpennen Ø9,6×11,5mm op 33mm h.o.h. De CNC maakt hiervoor Ø10mm doorlopende gaten vanaf de bovenzijde, in dezelfde opspanning als de staandergroeven. De korte Ø4-houtschroef wordt zonder CNC-voorboring gemonteerd.</div>
          <div class=""calcNote"">Een zijplint klikt op de voorste en achterste hoekpoot. Op de gedeelde voorhoek wordt één clip omgekeerd geplaatst. De voorplint wordt automatisch ingekort voor een rechte stompe hoekverbinding.</div>
<div class=""calcNote"">De fysiek goedgekeurde cliptong en inschuifkamer blijven ongewijzigd. Adapter V2 heeft een 38mm kern met een 6mm links/rechts gespiegelde montagevleugel. Het onderste Ø4,5-doorvoergat ligt onder de schuifbaan; het bovenste ligt 19mm zijwaarts op de vleugel. Beide krijgen een gemeten conische kopzitting Ø8,3×4,2mm voor verzonken Ø4-schroeven en liggen volledig buiten het schuifvlak. De plint krijgt uitgelijnde blinde CNC-pilotgaten Ø3×10mm. De vleugel wijst bij buitenhoeken altijd naar het midden van de plint. CAM gebruikt de Ø3mm 2-fluit carbidefrees voor gaten kleiner dan 6mm en de Ø6mm-frees voor grotere gaten, sleuven, verdiepingen en contouren.</div>
          <div class=""calcNote"">Iedere unitscheiding krijgt een normaal tussenschot. Achter een T-aanslag stopt het paneel 15mm achter de voorzijde en grijpt het 3mm in de centreersleuf. Alleen de scheiding tussen twee deurparen is dubbel.</div>
        </div>
        <div class=""row productOnlyCabinet productOnlyCabinetShelves"">
          <div><label>Legplanken per unit</label><input id=""defaultShelfCount"" type=""number"" value=""3"" min=""0""></div>
          <div><label>Legplanken starten</label><select id=""shelfStartMode""><option value=""bottom"">Start onder</option><option value=""top"" selected>Start boven</option></select></div>
        </div>
        <div class=""row productOnlyCabinet"">
          <div><label>Lades per unit</label><input id=""defaultDrawerCount"" type=""number"" value=""1"" min=""0""></div>
          <div><label>Legplanken verdiept mm</label><input id=""shelfFrontInsetMm"" type=""number"" value=""0"" min=""0""></div>
        </div>
        <div class=""productOnlyCabinetOrCubby"">
          <div class=""cabinetOnly""><label>Deuren</label><select id=""doorMode""><option value=""geen"">Open vakken</option><option value=""links"">Draaideur links</option><option value=""rechts"">Draaideur rechts</option><option value=""sliding"">Schuifdeuren</option></select></div>
          <div id=""slidingDoorOptions"" class=""cabinetOnly"">
            <label>Plaatmateriaal schuifdeuren</label><select id=""slidingDoorMaterialId""></select>
            <div class=""row"">
              <div><label>Schuifdeuren vanaf unit</label><input id=""slidingDoorStartUnit"" type=""number"" value=""1"" min=""1"" max=""12""></div>
              <div><label>Schuifdeuren t/m unit</label><input id=""slidingDoorEndUnit"" type=""number"" value=""4"" min=""1"" max=""12""></div>
            </div>
            <div class=""row"">
              <div><label>Overlap schuifdeur mm</label><input id=""slidingDoorOverlapMm"" type=""number"" value=""25"" min=""0"" max=""120""></div>
              <div><label>Benodigde legplankverdieping</label><input id=""slidingDoorRequiredInsetMm"" type=""number"" value=""46"" readonly></div>
            </div>
          </div>
          <div class=""checks"">
            <label><input id=""includeBackPanel"" type=""checkbox"" checked> Achterwand toevoegen</label>
            <label class=""cabinetOnly workbenchCabinetDrawerOption""><input id=""includeTopDrawer"" type=""checkbox""> Bovenlade per unit</label>
            <label class=""cabinetOnly workbenchCabinetDrawerOption""><input id=""includeDrawerPullCutouts"" type=""checkbox""> Uitgefreesde handgrepen lades</label>
            <label class=""cabinetOnly adjustableShelfOption""><input id=""includeAdjustableShelfHoles"" type=""checkbox"" checked> Legplankgaten systeem 32</label>
            <label class=""productOnlyWorkbenchCabinet""><input id=""testFitFirstSheet"" type=""checkbox""> Plaat 1 als volle passingstestplaat (zijwand + complete lade + deur)</label>
          </div>
        </div>
        <div class=""generateBar"">
          <div class=""generateActions"">
            <button id=""generateBtn"" type=""button"">Genereer kast</button>
            <button id=""solidWorksBtn"" class=""secondary"" type=""button"" disabled>Exporteer projectpakket</button>
          </div>
          <div class=""exportOptions"">
            <div class=""exportOptionsTitle""><span>Onderdelen projectexport</span><span>standaard alles geselecteerd</span></div>
            <div class=""exportOptionGrid"">
              <label><input id=""exportIncludeCam"" class=""exportOption"" type=""checkbox"" checked> CAM</label>
              <label><input id=""exportIncludeSolidWorks"" class=""exportOption"" type=""checkbox"" checked> SolidWorks</label>
              <label><input id=""exportIncludeCustomerPackage"" class=""exportOption"" type=""checkbox"" checked> Klantvoorstel</label>
              <label><input id=""exportIncludeThreeDPrint"" class=""exportOption"" type=""checkbox"" checked> 3D-print</label>
              <label><input id=""exportIncludeControls"" class=""exportOption"" type=""checkbox"" checked> Projectdata</label>
            </div>
          </div>
          <div class=""exportHint"">Alleen de aangevinkte onderdelen komen in de projectmap</div>
          <div id=""dirtyNote"" class=""dirtyNote"">Pas instellingen aan en genereer opnieuw.</div>
        </div>
      </section>
      <section class=""panel glass"">
        <h2>Klant</h2>
        <label>Naam</label><input id=""customerName"" placeholder=""Klant of organisatie"">
        <label>Projectnaam</label><input id=""projectName"" placeholder=""Bijvoorbeeld Workstation"">
        <label>Email</label><input id=""customerEmail"" placeholder=""naam@bedrijf.nl"">
        <label>Telefoon</label><input id=""customerPhone"">
        <label>Opmerking</label><textarea id=""notes""></textarea>
        <div class=""toolbar"">
          <button id=""quoteBtn"">Bereken prijs</button>
          <button id=""mailQuoteBtn"" class=""secondary"" disabled>Mail offerte</button>
          <button id=""orderBtn"" class=""warn"" disabled>Akkoord & order maken</button>
        </div>
        <p id=""message"" class=""muted""></p>
      </section>
    </div>

    <div class=""stack"">
      <section class=""panel glass"">
        <div class=""pricePanel"">
          <div>
            <h2>Prijs & visualisatie</h2>
            <div id=""summary"" class=""summaryLine"">Nog geen berekening.</div>
            <div id=""price"" class=""price"">-</div>
            <div id=""priceBreakdown"" class=""priceBreakdown""></div>
            <div id=""lead"" class=""lead""></div>
          </div>
          <span class=""badge"">Richtprijs MVP</span>
        </div>
      </section>
      <div class=""previewGrid"">
        <section class=""panel glass"">
          <div class=""sectionHead""><h3>360 assembly</h3><div class=""viewActions""><span class=""muted"">sleep of draai</span><button type=""button"" id=""toggleDoorsBtn"" onclick=""toggleDoors()"">Deuren verbergen</button><button type=""button"" id=""toggleTopBtn"" onclick=""toggleLexTop()"" style=""display:none"">Blad massief</button><button type=""button"" onclick=""openViewer('assembly')"">Vergroot</button></div></div>
          <div class=""lexViewerTools""><div class=""lexToolGroups""><div class=""lexViewButtons""><strong>Aanzicht</strong><button type=""button"" data-assembly-view=""iso"" onclick=""setAssemblyView('iso')"">Iso</button><button type=""button"" data-assembly-view=""front"" onclick=""setAssemblyView('front')"">Voor</button><button type=""button"" data-assembly-view=""side"" onclick=""setAssemblyView('side')"">Zij</button><button type=""button"" data-assembly-view=""underside"" onclick=""setAssemblyView('underside')"">Onderzijde</button></div><div class=""lexViewButtons""><strong>Kleur</strong><button type=""button"" data-assembly-color=""realistic"" onclick=""setAssemblyColorMode('realistic')"">Echte kleuren</button><button type=""button"" data-assembly-color=""technical"" onclick=""setAssemblyColorMode('technical')"">Constructiekleuren</button></div></div><div class=""lexLegend""><span><i class=""legendDot legendProfile""></i>profielen</span><span><i class=""legendDot legendLift""></i>HTE2</span><span><i class=""legendDot legendGuide""></i>HSR15/wagens</span><span><i class=""legendDot legendSheet""></i>platen</span></div></div>
          <div class=""canvasbox"">
            <canvas id=""assemblyCanvas"" width=""980"" height=""620""></canvas>
            <canvas id=""assemblyFallbackCanvas"" width=""980"" height=""620""></canvas>
            <div class=""viewerHint""><span>360 graden</span><input id=""rotation"" type=""range"" min=""0"" max=""360"" value=""215""><span id=""partCount"">0 onderdelen</span></div>
          </div>
        </section>
        <div class=""sidePreviews"">
          <section class=""panel glass"">
            <div class=""sectionHead""><h3>Productpreview</h3><div class=""viewActions""><span class=""muted"">front + zijaanzicht</span><button type=""button"" onclick=""openViewer('ortho')"">Vergroot</button></div></div>
            <div id=""productPreview"" class=""orthobox""><canvas id=""orthoCanvas"" width=""720"" height=""420""></canvas></div>
          </section>
          <section class=""panel glass"">
            <div class=""sectionHead""><h3>Nesting</h3><div class=""viewActions""><span class=""muted"">technisch</span><button type=""button"" onclick=""openViewer('nesting')"">Vergroot</button></div></div>
            <div id=""nestingPreview"" class=""svgbox""></div>
          </section>
        </div>
      </div>
      <section class=""panel glass"">
        <div class=""sectionHead""><h2>Werkplaats inbox</h2><button class=""secondary"" id=""refreshOrders"">Vernieuwen</button></div>
        <table><thead><tr><th>Order</th><th>Status</th><th>Klant</th><th></th></tr></thead><tbody id=""orders""></tbody></table>
      </section>
    </div>
  </main>

  <div class=""modal"" id=""viewerModal"">
    <div class=""modalPanel"">
      <div class=""modalHead""><h2 id=""modalTitle"">Visualisatie</h2><div class=""modalHeadActions""><div id=""modalAssemblyTools"" class=""modalAssemblyTools""><button type=""button"" data-assembly-view=""iso"" onclick=""setAssemblyView('iso')"">Iso</button><button type=""button"" data-assembly-view=""front"" onclick=""setAssemblyView('front')"">Voor</button><button type=""button"" data-assembly-view=""side"" onclick=""setAssemblyView('side')"">Zij</button><button type=""button"" data-assembly-view=""underside"" onclick=""setAssemblyView('underside')"">Onderzijde</button><span class=""modalToolDivider""></span><button type=""button"" data-assembly-color=""realistic"" onclick=""setAssemblyColorMode('realistic')"">Echte kleuren</button><button type=""button"" data-assembly-color=""technical"" onclick=""setAssemblyColorMode('technical')"">Constructie</button></div><button class=""ghost"" type=""button"" onclick=""closeViewer()"">Sluiten</button></div></div>
      <div class=""modalBody"" id=""modalBody""></div>
    </div>
  </div>

  <script>
    let lastRequest=null,lastQuote=null,assemblyParts=[],hideDoors=false,ghostLexTop=false,assemblyViewMode='iso',assemblyColorMode='realistic',rotationDeg=215,dragging=false,lastDragX=0,threePromise=null,threeApi=null,threeState=null,modalSource=null,nestingZoom=1,nestingBaseWidth=0,nestingBaseHeight=0,catalogData=null;
    const $=id=>document.getElementById(id);
    async function api(path,opts){const r=await fetch(path,opts),text=await r.text();let data=null;try{data=text?JSON.parse(text):null;}catch(e){}if(!r.ok)throw new Error((data&&(data.error||data.Error||data.message||data.Message))||text||('HTTP-fout '+r.status));return data;}
    async function stopPortal(){const btn=document.querySelector('.stopPortal');if(btn){btn.disabled=true;btn.textContent='Stopt...';}try{const r=await api('/api/shutdown',{method:'POST'});document.body.innerHTML='<main style=""display:grid;place-items:center;min-height:100vh""><section class=""panel"" style=""max-width:520px;text-align:center""><h2>Portal gestopt</h2><p class=""muted"">'+r.message+'</p></section></main>';}catch(e){document.body.innerHTML='<main style=""display:grid;place-items:center;min-height:100vh""><section class=""panel"" style=""max-width:520px;text-align:center""><h2>Portal is gestopt</h2><p class=""muted"">Start de configurator opnieuw om verder te gaan.</p></section></main>';}}
    function productMeta(product){return catalogData&&catalogData.products?(catalogData.products.find(x=>x.Product===product)||null):null;}
    function selectedSheetThickness(){const id=$('sheetMaterialId')?$('sheetMaterialId').value:null,item=catalogData&&catalogData.sheets?catalogData.sheets.find(x=>x.Id===id):null;return item&&item.ThicknessMm>0?item.ThicknessMm:18;}
    function roundMm(value){return Math.round(value*10)/10;}
    function syncWorkbenchCabinetFootGeometry(){if(!$('workbenchCabinetCalculatedFrontFootInsetMm'))return;const setback=Math.max(0,+$('workbenchCabinetPlinthSetbackMm').value||0),clip=Math.max(0,+$('workbenchCabinetPlinthClipCenterBehindBackFaceMm').value||0),thickness=selectedSheetThickness(),clearance=1,adapterBack=3,frontAxis=Math.max(clip+adapterBack,51/2+clearance),sideAxis=Math.max(clip+adapterBack,Math.max(47,76-47)+clearance);$('workbenchCabinetCalculatedFrontFootInsetMm').value=roundMm(setback+thickness+frontAxis);if($('workbenchCabinetCalculatedSideFootInsetMm'))$('workbenchCabinetCalculatedSideFootInsetMm').value=roundMm(thickness+sideAxis);if($('workbenchCabinetCalculatedFrontAdapterStandOffMm'))$('workbenchCabinetCalculatedFrontAdapterStandOffMm').value=roundMm(frontAxis-clip);if($('workbenchCabinetCalculatedSideAdapterStandOffMm'))$('workbenchCabinetCalculatedSideAdapterStandOffMm').value=roundMm(sideAxis-clip);}
    function setCubbyDefaults(meta){const t=selectedSheetThickness(),cols=Math.max(1,+(meta&&meta.DefaultUnitCount||3)),rows=Math.max(1,+(meta&&meta.DefaultShelfCount||4));$('cubbyColumnCount').value=cols;$('cubbyRowCount').value=rows;$('cubbyCellWidthMm').value=roundMm(((meta&&meta.DefaultWidthMm?meta.DefaultWidthMm:1272)-(cols+1)*t)/cols);$('cubbyCellDepthMm').value=350;$('cubbyCellHeightMm').value=roundMm(((meta&&meta.DefaultHeightMm?meta.DefaultHeightMm:1490)-(rows+1)*t)/rows);$('cubbyGridInsetMm').value=20;}
    function syncCubbyDimensions(){if($('product').value!=='vakjeskast')return;const t=selectedSheetThickness(),cols=Math.max(1,+$('cubbyColumnCount').value||1),rows=Math.max(1,+$('cubbyRowCount').value||1),cellW=Math.max(40,+$('cubbyCellWidthMm').value||400),cellD=Math.max(40,+$('cubbyCellDepthMm').value||350),cellH=Math.max(40,+$('cubbyCellHeightMm').value||350),frontInset=Math.max(0,+$('cubbyGridInsetMm').value||0);$('widthMm').value=roundMm(cols*cellW+(cols+1)*t);$('depthMm').value=roundMm(cellD+frontInset+t);$('heightMm').value=roundMm(rows*cellH+(rows+1)*t);$('unitCount').value=cols;$('defaultShelfCount').value=rows;if($('cubbyCombCount'))$('cubbyCombCount').textContent='Interne kamdelen: '+Math.max(0,cols-1)+' staander-kammen, '+Math.max(0,rows-1)+' ligger-kammen. Buitenwanden en boven/bodem zijn aparte kastdelen.';}
    function chooseProduct(product){document.body.classList.add('appOn');$('product').value=product;$('quantity').value=1;const meta=productMeta(product);if(meta){$('widthMm').value=meta.DefaultWidthMm;$('depthMm').value=meta.DefaultDepthMm;$('heightMm').value=meta.DefaultHeightMm;$('unitCount').value=meta.DefaultUnitCount;$('defaultShelfCount').value=meta.DefaultShelfCount;$('defaultDrawerCount').value=meta.DefaultDrawerCount;$('shelfStartMode').value=meta.DefaultShelfStartMode||'bottom';if(product==='vakjeskast')setCubbyDefaults(meta);}else if(product==='werktafel'){$('widthMm').value=1500;$('depthMm').value=750;$('heightMm').value=900;$('unitCount').value=1;$('defaultShelfCount').value=0;$('defaultDrawerCount').value=0;}else if(product==='vakjeskast'){setCubbyDefaults(null);}else{$('widthMm').value=2400;$('depthMm').value=600;$('heightMm').value=900;$('unitCount').value=4;$('defaultShelfCount').value=3;$('shelfStartMode').value='top';$('defaultDrawerCount').value=1;}if(product==='werkbankkast'){$('workbenchCabinetPlinthHeightMm').value=114;$('workbenchCabinetPlinthSetbackMm').value=45;$('workbenchCabinetIncludeLeftSidePlinth').checked=false;$('workbenchCabinetIncludeRightSidePlinth').checked=false;$('workbenchCabinetFootInsetMm').value=55;$('workbenchCabinetPlinthClipCenterBehindBackFaceMm').value=25.4;$('workbenchCabinetDoorStopWidthMm').value=50;$('workbenchCabinetTopDrawerHeightMm').value=160;$('workbenchCabinetFrontPanelCornerRadiusMm').value=2;$('workbenchCabinetShelfPositionCount').value=6;$('workbenchCabinetShelfFrontInsetMm').value=0;$('includeBackPanel').checked=true;$('includeAdjustableShelfHoles').checked=true;$('includeTopDrawer').checked=false;}syncProductUi();quote();}
    function backToStart(){document.body.classList.remove('appOn');}
    function syncProductUi(){const product=$('product').value,isRevolution=product==='werktafel_lex_revolution',isWorkbench=product==='werktafel'||product==='werktafel_lex'||isRevolution,isLex=product==='werktafel_lex'||isRevolution,isCubby=product==='vakjeskast',isWorkbenchCabinet=product==='werkbankkast',meta=productMeta(product);$('productName').textContent=meta&&meta.Name?meta.Name:(isWorkbench?'Werktafel':(isCubby?'Vakjeskast':(isWorkbenchCabinet?'Werkbank met kastonderbouw':'Cabinet / kast')));if(isLex&&$('projectName')&&!$('projectName').value.trim())$('projectName').value='Workstation';document.body.classList.toggle('isWorkbench',isWorkbench);document.body.classList.toggle('isLex',isLex);document.body.classList.toggle('isCubby',isCubby);document.body.classList.toggle('isWorkbenchCabinet',isWorkbenchCabinet);$('widthLabel').textContent=isCubby?'Buitenbreedte mm':'Breedte mm';$('depthLabel').textContent=isCubby?'Buitendiepte mm':'Diepte mm';$('heightLabel').textContent=isCubby?'Buitenhoogte mm':'Hoogte mm';$('widthMm').readOnly=isCubby;$('depthMm').readOnly=isCubby;$('heightMm').readOnly=isCubby;if(isCubby)syncCubbyDimensions();syncSlidingDoorUi();syncWorkbenchCabinetFootGeometry();$('generateBtn').textContent=isRevolution?'Genereer ontwikkelvariant':(isLex?'Genereer workstation':(isWorkbench?'Genereer tafel':(isCubby?'Genereer vakjeskast':(isWorkbenchCabinet?'Genereer kastonderbouw':'Genereer kast'))));markDirty();}
    function syncSlidingDoorUi(){const sliding=$('doorMode')&&$('doorMode').value==='sliding',units=Math.max(1,+$('unitCount').value||1),required=46;document.body.classList.toggle('slidingDoorMode',sliding);if(!$('slidingDoorStartUnit'))return;$('slidingDoorStartUnit').max=units;$('slidingDoorEndUnit').max=units;if(+$('slidingDoorStartUnit').value<1)$('slidingDoorStartUnit').value=1;if(+$('slidingDoorEndUnit').value<1||+$('slidingDoorEndUnit').value>units)$('slidingDoorEndUnit').value=units;if(+$('slidingDoorStartUnit').value>units)$('slidingDoorStartUnit').value=units;$('slidingDoorRequiredInsetMm').value=required;if(sliding){$('shelfFrontInsetMm').min=required;if((+$('shelfFrontInsetMm').value||0)<required)$('shelfFrontInsetMm').value=required;}else{$('shelfFrontInsetMm').min=0;}}
    function markDirty(){if($('orderBtn'))$('orderBtn').disabled=true;if($('mailQuoteBtn'))$('mailQuoteBtn').disabled=true;if($('solidWorksBtn'))$('solidWorksBtn').disabled=true;lastQuote=null;if($('dirtyNote'))$('dirtyNote').textContent='Instellingen gewijzigd. Genereer opnieuw voor actuele prijs en 3D assembly.';}
    function request(){syncCubbyDimensions();syncSlidingDoorUi();const product=$('product').value,isWorkbenchCabinet=product==='werkbankkast',isLex=product==='werktafel_lex'||product==='werktafel_lex_revolution';return{product:product,widthMm:+$('widthMm').value,depthMm:+$('depthMm').value,heightMm:+$('heightMm').value,quantity:Math.max(1,+$('quantity').value||1),unitCount:+$('unitCount').value,sheetMaterialId:isLex?'hpl_10_lex':$('sheetMaterialId').value,drawerMaterialId:$('drawerMaterialId').value,backMaterialId:$('backMaterialId').value,slidingDoorMaterialId:$('slidingDoorMaterialId')?$('slidingDoorMaterialId').value:'betonplex_12',profileMaterialId:isLex?'alu_system_80x80':$('profileMaterialId').value,profileSawingMode:$('profileSawingMode').value,includeBackPanel:$('includeBackPanel').checked,includeTopDrawer:$('includeTopDrawer').checked,includeDrawerPullCutouts:$('includeDrawerPullCutouts').checked,includeAdjustableShelfHoles:$('includeAdjustableShelfHoles').checked,defaultShelfCount:+$('defaultShelfCount').value,adjustableShelfPositionCount:isWorkbenchCabinet?+$('workbenchCabinetShelfPositionCount').value:0,shelfStartMode:$('shelfStartMode').value,shelfFrontInsetMm:isWorkbenchCabinet?+$('workbenchCabinetShelfFrontInsetMm').value:+$('shelfFrontInsetMm').value,defaultDrawerCount:+$('defaultDrawerCount').value,doorMode:$('doorMode').value,slidingDoorStartUnit:$('slidingDoorStartUnit')?+$('slidingDoorStartUnit').value:1,slidingDoorEndUnit:$('slidingDoorEndUnit')?+$('slidingDoorEndUnit').value:+$('unitCount').value,slidingDoorOverlapMm:$('slidingDoorOverlapMm')?+$('slidingDoorOverlapMm').value:25,customerName:$('customerName').value,projectName:$('projectName').value,customerEmail:$('customerEmail').value,customerPhone:$('customerPhone').value,notes:$('notes').value,includeLowerShelf:$('includeLowerShelf').checked,includeMiddleShelf:$('includeMiddleShelf').checked,lowerShelfHeightMm:+$('lowerShelfHeightMm').value,middleLayerHeightMm:+$('middleShelfHeightMm').value,middleShelfHeightMm:+$('middleShelfHeightMm').value,cubbyColumnCount:+$('cubbyColumnCount').value,cubbyRowCount:+$('cubbyRowCount').value,cubbyCellWidthMm:+$('cubbyCellWidthMm').value,cubbyCellDepthMm:+$('cubbyCellDepthMm').value,cubbyCellHeightMm:+$('cubbyCellHeightMm').value,cubbyGridInsetMm:+$('cubbyGridInsetMm').value,workbenchCabinetPlinthHeightMm:+$('workbenchCabinetPlinthHeightMm').value,workbenchCabinetPlinthSetbackMm:+$('workbenchCabinetPlinthSetbackMm').value,workbenchCabinetIncludeLeftSidePlinth:$('workbenchCabinetIncludeLeftSidePlinth').checked,workbenchCabinetIncludeRightSidePlinth:$('workbenchCabinetIncludeRightSidePlinth').checked,workbenchCabinetFootInsetMm:+$('workbenchCabinetFootInsetMm').value,workbenchCabinetPlinthClipCenterBehindBackFaceMm:+$('workbenchCabinetPlinthClipCenterBehindBackFaceMm').value,workbenchCabinetDoorStopWidthMm:+$('workbenchCabinetDoorStopWidthMm').value,workbenchCabinetTopDrawerHeightMm:+$('workbenchCabinetTopDrawerHeightMm').value,testFitFirstSheet:$('testFitFirstSheet').checked,workbenchCabinetFrontPanelCornerRadiusMm:+$('workbenchCabinetFrontPanelCornerRadiusMm').value};}
    async function loadCatalog(){const c=await api('/api/catalog');catalogData=c;const sheetOptions=c.sheets.map(x=>`<option value=""${x.Id}"">${x.Name}</option>`).join('');$('sheetMaterialId').innerHTML=sheetOptions;$('drawerMaterialId').innerHTML=sheetOptions;$('backMaterialId').innerHTML=sheetOptions;if($('slidingDoorMaterialId'))$('slidingDoorMaterialId').innerHTML=sheetOptions;$('profileMaterialId').innerHTML=c.profiles.map(x=>`<option value=""${x.Id}"">${x.Name}</option>`).join('');$('sheetMaterialId').value='betonplex_18';$('drawerMaterialId').value='multiplex_15';$('backMaterialId').value='multiplex_15';if($('slidingDoorMaterialId'))$('slidingDoorMaterialId').value='betonplex_12';$('profileMaterialId').value='alu_profile_40x40';}
    async function quote(){try{$('message').textContent='Genereren...';$('generateBtn').disabled=true;lastRequest=request();lastQuote=await api('/api/quote',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(lastRequest)});$('summary').textContent=lastQuote.Summary;$('price').textContent='EUR '+Number(lastQuote.PriceIncVat).toFixed(2)+' incl. btw';renderPriceBreakdown(lastQuote);$('lead').textContent=lastQuote.LeadTime+' - excl. btw EUR '+Number(lastQuote.PriceExVat).toFixed(2);$('nestingPreview').innerHTML=lastQuote.NestingSvg;resetNestingZoom();assemblyParts=lastQuote.Assembly3D||[];updatePartCount();renderAssembly();renderOrthoPreview();loadThreeViewer().then(()=>{renderAssembly();renderOrthoPreview();});$('orderBtn').disabled=false;$('mailQuoteBtn').disabled=false;$('solidWorksBtn').disabled=false;$('dirtyNote').textContent='Actuele configuratie gegenereerd.';$('message').textContent='Prijs klaar. Controleer preview en zet bij akkoord om naar order.';}catch(e){$('message').innerHTML='<span class=""error"">'+e.message+'</span>';}finally{$('generateBtn').disabled=false;}}
    function renderPriceBreakdown(q){const money=v=>'EUR '+Number(v||0).toFixed(2);$('priceBreakdown').innerHTML=[['Plaatmateriaal ex',q.Material],['Beslag ex',q.Hardware],['Machine ex',q.Machine],['Arbeid ex',q.Labour],['Opslag/marge',q.Margin],['Btw',q.Vat]].map(x=>`<span>${x[0]}<strong>${money(x[1])}</strong></span>`).join('');}
    function visibleAssemblyParts(){return hideDoors?assemblyParts.filter(p=>!isDoorPanelPart(p)):assemblyParts;}
    function updatePartCount(){const parts=visibleAssemblyParts();if($('partCount'))$('partCount').textContent=parts.length+' onderdelen';const hasDoors=assemblyParts.some(isDoorPanelPart),doorBtn=$('toggleDoorsBtn');if(doorBtn){doorBtn.style.display=hasDoors?'':'none';doorBtn.textContent=hideDoors?'Deuren tonen':'Deuren verbergen';}const hasLexTop=assemblyParts.some(p=>(p.Name||'').toLowerCase().includes('kogelpotblad')),topBtn=$('toggleTopBtn');if(topBtn){topBtn.style.display=hasLexTop?'':'none';topBtn.textContent=ghostLexTop?'Blad massief':'Blad transparant';}document.body.classList.toggle('realAssemblyColors',assemblyColorMode==='realistic');document.querySelectorAll('[data-assembly-view]').forEach(btn=>btn.classList.toggle('active',btn.dataset.assemblyView===assemblyViewMode));document.querySelectorAll('[data-assembly-color]').forEach(btn=>btn.classList.toggle('active',btn.dataset.assemblyColor===assemblyColorMode));}
    function toggleDoors(){hideDoors=!hideDoors;updatePartCount();if(threeState)threeState.lastKey='';renderAssembly();renderOrthoPreview();}
    function toggleLexTop(){ghostLexTop=!ghostLexTop;updatePartCount();if(threeState)threeState.lastKey='';renderAssembly();renderOrthoPreview();}
    function setAssemblyView(mode){assemblyViewMode=mode;rotationDeg=mode==='side'?90:(mode==='front'?180:215);$('rotation').value=Math.round(rotationDeg);updatePartCount();if(threeState){threeState.forceFit=true;threeState.lastKey='';}renderAssembly();}
    function setAssemblyColorMode(mode){assemblyColorMode=mode==='technical'?'technical':'realistic';updatePartCount();if(threeState)threeState.lastKey='';renderAssembly();renderOrthoPreview();}
    function mailQuote(){if(!lastQuote||!lastRequest){$('message').textContent='Genereer eerst een actuele offerte.';return;}const subject='Offerte '+lastQuote.ProductName+' - '+lastQuote.QuoteId;const body=['Beste '+(lastRequest.CustomerName||'klant')+',','','Hierbij de richtofferte voor je configuratie.','',lastQuote.Summary,'Prijs excl. btw: EUR '+Number(lastQuote.PriceExVat).toFixed(2),'Btw: EUR '+Number(lastQuote.Vat).toFixed(2),'Prijs incl. btw: EUR '+Number(lastQuote.PriceIncVat).toFixed(2),lastQuote.LeadTime,'','Let op: dit is een MVP-richtprijs. Na technische controle bevestigen wij de definitieve productiegegevens.','','Met vriendelijke groet,','SW Werkplaats'].join('\\n');window.location.href='mailto:'+(lastRequest.CustomerEmail||'')+'?subject='+encodeURIComponent(subject)+'&body='+encodeURIComponent(body);}
    async function order(){try{if(!lastRequest)await quote();$('message').textContent='Order maken...';const o=await api('/api/orders',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(lastRequest)});$('message').textContent=o.Message+' '+o.Order.OrderId;await loadOrders();}catch(e){$('message').innerHTML='<span class=""error"">'+e.message+'</span>';}}
    function exportSelection(){return{exportIncludeCam:$('exportIncludeCam').checked,exportIncludeSolidWorks:$('exportIncludeSolidWorks').checked,exportIncludeCustomerPackage:$('exportIncludeCustomerPackage').checked,exportIncludeThreeDPrint:$('exportIncludeThreeDPrint').checked,exportIncludeControls:$('exportIncludeControls').checked};}
    async function exportSolidWorks(){if(!lastRequest||!lastQuote){$('message').textContent='Genereer eerst de actuele configuratie.';return;}const selection=exportSelection(),selected=Object.values(selection).filter(Boolean).length;if(!selected){$('message').innerHTML='<span class=""error"">Selecteer minimaal één onderdeel voor de projectexport.</span>';return;}try{$('solidWorksBtn').disabled=true;$('dirtyNote').textContent='Geselecteerde projectonderdelen worden opgebouwd; dit kan enkele minuten duren...';$('message').textContent='De geselecteerde CAM-, SOLIDWORKS-, klantvoorstel-, 3D-print- en projectdatabestanden worden in één projectmap opgebouwd. SOLIDWORKS Design wordt alleen gestart wanneer SolidWorks of Klantvoorstel is geselecteerd; rond een eventuele login zelf af.';const exportRequest=Object.assign({},lastRequest,selection),r=await api('/api/solidworks/export',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(exportRequest)});await api('/api/output/open-folder',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({path:r.OutputFolder})});$('dirtyNote').textContent='Export gereed. De projectmap is geopend in Verkenner.';const details=[];if(r.PartCount)details.push(r.PartCount+' SolidWorks-parts');details.push(r.PlacementCount+' plaatsingen');details.push(r.FileCount+' bestanden totaal');$('message').textContent=r.Message+' '+details.join(' / ')+'.';}catch(e){$('dirtyNote').textContent='Export niet voltooid.';$('message').innerHTML='<span class=""error"">'+e.message+'</span>';}finally{$('solidWorksBtn').disabled=false;}}
    async function release(id){await api('/api/orders/'+encodeURIComponent(id)+'/release',{method:'POST'});await loadOrders();}
    async function loadOrders(){const list=await api('/api/orders');$('orders').innerHTML=list.map(o=>`<tr><td>${o.OrderId}<br><span class=""muted"">${o.ProductName}</span></td><td><span class=""pill"">${o.Status}</span></td><td>${o.CustomerName||''}</td><td class=""orderTools""><button class=""secondary"" onclick=""release('${o.OrderId}')"">Vrijgeven</button></td></tr>`).join('')||'<tr><td colspan=""4"" class=""muted"">Nog geen orders.</td></tr>';}
    $('quoteBtn').onclick=quote;$('generateBtn').onclick=quote;$('mailQuoteBtn').onclick=mailQuote;$('solidWorksBtn').onclick=exportSolidWorks;$('orderBtn').onclick=order;$('refreshOrders').onclick=loadOrders;
    document.querySelectorAll('#configPanel input:not(.exportOption),#configPanel select').forEach(el=>{const cubbyDriver=['cubbyCellWidthMm','cubbyCellDepthMm','cubbyCellHeightMm','cubbyColumnCount','cubbyRowCount','cubbyGridInsetMm','sheetMaterialId'].includes(el.id),slidingDriver=['doorMode','unitCount','slidingDoorStartUnit','slidingDoorEndUnit'].includes(el.id);el.addEventListener('input',()=>{if(cubbyDriver)syncCubbyDimensions();if(slidingDriver)syncSlidingDoorUi();syncWorkbenchCabinetFootGeometry();markDirty();});el.addEventListener('change',()=>{if(el.id==='product')syncProductUi();else{if(cubbyDriver)syncCubbyDimensions();if(slidingDriver)syncSlidingDoorUi();syncWorkbenchCabinetFootGeometry();markDirty();}});});
    document.querySelectorAll('input[type=number]').forEach(el=>{let spinStart=null,spinActive=false;el.addEventListener('wheel',e=>{if(document.activeElement===el)e.preventDefault();},{passive:false});el.addEventListener('mousedown',()=>{spinStart=el.value;spinActive=true;});const stopSpin=()=>{if(spinActive&&el.value!==spinStart&&document.activeElement===el)el.blur();spinActive=false;spinStart=null;};el.addEventListener('mouseup',stopSpin);el.addEventListener('mouseleave',e=>{if(e.buttons)stopSpin();});});
    $('rotation').oninput=e=>{rotationDeg=+e.target.value;renderAssembly();};
    const canvas=$('assemblyCanvas');
    canvas.onmousedown=e=>{dragging=true;lastDragX=e.clientX;};
    window.onmouseup=()=>dragging=false;
    window.onmousemove=e=>{if(!dragging)return;rotationDeg=(rotationDeg+(e.clientX-lastDragX)*.6+360)%360;lastDragX=e.clientX;$('rotation').value=Math.round(rotationDeg);renderAssembly();};
    canvas.ontouchstart=e=>{dragging=true;lastDragX=e.touches[0].clientX;};
    canvas.ontouchend=()=>dragging=false;
    canvas.ontouchmove=e=>{if(!dragging)return;rotationDeg=(rotationDeg+(e.touches[0].clientX-lastDragX)*.6+360)%360;lastDragX=e.touches[0].clientX;$('rotation').value=Math.round(rotationDeg);renderAssembly();};
    window.onresize=renderAssembly;
    window.addEventListener('resize',renderOrthoPreview);
    function openViewer(kind){const body=$('modalBody');body.innerHTML='';modalSource=kind;$('modalAssemblyTools').style.display=kind==='assembly'&&document.body.classList.contains('isLex')?'flex':'none';if(kind==='assembly'){body.appendChild(document.querySelector('.canvasbox'));$('modalTitle').textContent='360 assembly';}else if(kind==='ortho'){body.appendChild($('productPreview'));$('modalTitle').textContent='Voor- en zijaanzicht';}else{body.appendChild($('nestingPreview'));$('nestingPreview').classList.add('nestingZoomHost');$('modalTitle').textContent='Nesting';applyNestingZoom();}document.body.classList.add('modalOn');setTimeout(()=>{renderAssembly();renderOrthoPreview();if(kind==='nesting')applyNestingZoom();},40);}
    function closeViewer(){const kind=modalSource;if(kind==='assembly'){document.querySelector('.previewGrid > section').appendChild(document.querySelector('.canvasbox'));}else if(kind==='ortho'){document.querySelector('.sidePreviews section:first-child').appendChild($('productPreview'));}else if(kind==='nesting'){$('nestingPreview').classList.remove('nestingZoomHost');resetNestingZoom();document.querySelector('.sidePreviews section:nth-child(2)').appendChild($('nestingPreview'));}document.body.classList.remove('modalOn');modalSource=null;setTimeout(()=>{renderAssembly();renderOrthoPreview();},40);}
    function resetNestingZoom(){nestingZoom=1;nestingBaseWidth=0;nestingBaseHeight=0;applyNestingZoom();}
    function applyNestingZoom(){const box=$('nestingPreview'),svg=box?box.querySelector('svg'):null;if(!svg)return;if(!nestingBaseWidth){nestingBaseWidth=parseFloat(svg.getAttribute('width'))||svg.viewBox.baseVal.width||svg.getBoundingClientRect().width;nestingBaseHeight=parseFloat(svg.getAttribute('height'))||svg.viewBox.baseVal.height||svg.getBoundingClientRect().height;}if(box.classList.contains('nestingZoomHost')){svg.style.width=(nestingBaseWidth*nestingZoom)+'px';svg.style.height=(nestingBaseHeight*nestingZoom)+'px';}else{svg.style.width='';svg.style.height='';}}
    $('nestingPreview').addEventListener('wheel',e=>{if(modalSource!=='nesting'||!e.ctrlKey)return;e.preventDefault();const box=$('nestingPreview'),rect=box.getBoundingClientRect(),oldZoom=nestingZoom,mouseX=e.clientX-rect.left+box.scrollLeft,mouseY=e.clientY-rect.top+box.scrollTop;nestingZoom=Math.max(.25,Math.min(6,nestingZoom*(e.deltaY<0?1.12:.89)));applyNestingZoom();const ratio=nestingZoom/oldZoom;box.scrollLeft=mouseX*ratio-(e.clientX-rect.left);box.scrollTop=mouseY*ratio-(e.clientY-rect.top);},{passive:false});
    function renderAssembly(){
      if(threeApi){renderThreeAssembly();return;}
      const c=$('assemblyFallbackCanvas'),ctx=c.getContext('2d'),box=c.getBoundingClientRect(),dpr=window.devicePixelRatio||1;
      const w=Math.max(520,Math.floor(box.width*dpr)),h=Math.max(420,Math.floor(box.height*dpr));if(c.width!==w||c.height!==h){c.width=w;c.height=h;}
      ctx.clearRect(0,0,w,h);const g=ctx.createLinearGradient(0,0,0,h);g.addColorStop(0,'#ffffff');g.addColorStop(1,'#eef2f7');ctx.fillStyle=g;ctx.fillRect(0,0,w,h);
      const parts=visibleAssemblyParts();
      if(!parts.length){ctx.fillStyle='#6e6e73';ctx.font=`${16*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText('Bereken eerst een configuratie voor de 360 assembly.',34*dpr,54*dpr);return;}
      let minX=1e9,maxX=-1e9,minY=1e9,maxY=-1e9,minZ=1e9,maxZ=-1e9;
      parts.forEach(p=>{minX=Math.min(minX,p.Xmm-p.SizeXmm/2);maxX=Math.max(maxX,p.Xmm+p.SizeXmm/2);minY=Math.min(minY,p.Ymm-p.SizeYmm/2);maxY=Math.max(maxY,p.Ymm+p.SizeYmm/2);minZ=Math.min(minZ,p.Zmm-p.SizeZmm/2);maxZ=Math.max(maxZ,p.Zmm+p.SizeZmm/2);});
      const cx=(minX+maxX)/2,cz=(minZ+maxZ)/2,cy=minY;const span=Math.max(maxX-minX,maxZ-minZ,(maxY-minY)*1.25,1);const scale=Math.min(w*.72/span,h*.70/span);const ang=rotationDeg*Math.PI/180,pitch=-.34;
      function rot(pt){let x=pt.x-cx,y=pt.y-cy,z=pt.z-cz;let rx=x*Math.cos(ang)-z*Math.sin(ang),rz=x*Math.sin(ang)+z*Math.cos(ang);let ry=y*Math.cos(pitch)-rz*Math.sin(pitch);rz=y*Math.sin(pitch)+rz*Math.cos(pitch);return{x:rx,y:ry,z:rz};}
      function proj(pt){const r=rot(pt),persp=900/(900+r.z*.22);return{x:w/2+r.x*scale*persp,y:h*.70-r.y*scale*persp,z:r.z};}
      const faces=[];parts.forEach((p,i)=>addBoxFaces(faces,p,i,proj));
      faces.sort((a,b)=>a.depth-b.depth);
      ctx.save();ctx.shadowColor='rgba(15,23,42,.13)';ctx.shadowBlur=10*dpr;ctx.shadowOffsetY=5*dpr;
      faces.forEach(f=>{ctx.beginPath();f.points.forEach((pt,i)=>i?ctx.lineTo(pt.x,pt.y):ctx.moveTo(pt.x,pt.y));ctx.closePath();ctx.fillStyle=f.fill;ctx.fill();ctx.shadowColor='transparent';ctx.strokeStyle='rgba(71,84,103,.45)';ctx.lineWidth=1*dpr;ctx.stroke();ctx.shadowColor='rgba(15,23,42,.10)';});
      ctx.restore();drawGround(ctx,w,h,dpr);ctx.fillStyle='#1d1d1f';ctx.font=`${18*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText('Volledige assembly 3D',24*dpr,34*dpr);ctx.fillStyle='#6e6e73';ctx.font=`${13*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText('Sleep horizontaal of gebruik de slider voor 360 graden rotatie.',24*dpr,56*dpr);
    }
    function addBoxFaces(faces,p,index,proj){
      const x=p.Xmm,y=p.Ymm,z=p.Zmm,sx=p.SizeXmm/2,sy=p.SizeYmm/2,sz=p.SizeZmm/2;
      const v=[{x:x-sx,y:y-sy,z:z-sz},{x:x+sx,y:y-sy,z:z-sz},{x:x+sx,y:y+sy,z:z-sz},{x:x-sx,y:y+sy,z:z-sz},{x:x-sx,y:y-sy,z:z+sz},{x:x+sx,y:y-sy,z:z+sz},{x:x+sx,y:y+sy,z:z+sz},{x:x-sx,y:y+sy,z:z+sz}].map(proj);
      const style=assemblyPartStyle(p),base=[shadeHex(style.css,-10),shadeHex(style.css,-20),shadeHex(style.css,10)];
      [[0,1,2,3,base[1]],[4,5,6,7,base[0]],[3,2,6,7,base[2]],[0,4,7,3,base[0]],[1,5,6,2,base[1]],[0,1,5,4,base[1]]].forEach(f=>faces.push({points:[v[f[0]],v[f[1]],v[f[2]],v[f[3]]],depth:(v[f[0]].z+v[f[1]].z+v[f[2]].z+v[f[3]].z)/4+index*.001,fill:f[4]}));
    }
    function shadeHex(hex,amount){const n=parseInt(hex.slice(1),16),r=Math.max(0,Math.min(255,(n>>16)+amount)),g=Math.max(0,Math.min(255,((n>>8)&255)+amount)),b=Math.max(0,Math.min(255,(n&255)+amount));return'#'+[r,g,b].map(v=>v.toString(16).padStart(2,'0')).join('');}
    function assemblyPartStyle(p){const partName=((p&&p.Name)||'').toLowerCase();if(p&&(p.Shape==='pa40-hinge'||(p.Shape||'').startsWith('black-hole-')||partName.includes('zwarte eindkap')||partName.includes('zwarte spleet')||partName.includes('liangyue ly103-12 clip')))return{hex:0x090b0d,css:'#090b0d',opacity:1,metalness:.38};if(p&&((p.Kind||'').startsWith('hardware-cabinet')))return{hex:0xc5c7c4,css:'#c5c7c4',opacity:1,metalness:.18};if(p&&p.Shape==='acrylic-panel')return assemblyColorMode==='technical'?{hex:0x8fd8f2,css:'#8fd8f2',opacity:.28,metalness:.02}:{hex:0xd9f3fb,css:'#d9f3fb',opacity:.22,metalness:.01};return assemblyColorMode==='technical'?technicalAssemblyPartStyle(p):realisticAssemblyPartStyle(p);}
    function technicalAssemblyPartStyle(p){const name=((p&&p.Name)||'').toLowerCase();if(name.includes('kogelpotblad'))return{hex:0xe9edf2,css:'#e9edf2',opacity:ghostLexTop ? .28 : 1,metalness:.02};if(name.includes('afdekkap'))return{hex:0x171a1e,css:'#171a1e',opacity:1,metalness:.08};if(name.includes('stabilisatieplaat'))return{hex:0xd6a348,css:'#d6a348',opacity:.96,metalness:.02};if(name.includes('hoekadapter')||name.includes('adapterplaat'))return{hex:0x24a19c,css:'#24a19c',opacity:1,metalness:.32};if(name.includes('hte2'))return{hex:0x1f4b73,css:'#1f4b73',opacity:.98,metalness:.28};if(name.includes('hsr15r wagen'))return{hex:0xf28c28,css:'#f28c28',opacity:1,metalness:.36};if(name.includes('hsr15 rail'))return{hex:0x697887,css:'#697887',opacity:1,metalness:.52};if(name.includes('eindstop'))return{hex:0xc94b50,css:'#c94b50',opacity:1,metalness:.28};if(name.includes('vast railframe'))return{hex:0x6e88a2,css:'#6e88a2',opacity:1,metalness:.3};if(name.includes('schuifframe')||name.includes('meebewegend')||name.includes('tafelbladframe'))return{hex:0x9eb5c9,css:'#9eb5c9',opacity:1,metalness:.3};if(name.includes('voetprofiel'))return{hex:0x536779,css:'#536779',opacity:1,metalness:.32};if(name.includes('kogelpot'))return{hex:0x4f5b66,css:'#4f5b66',opacity:1,metalness:.55};if(p.Kind==='sheet')return{hex:0xeadcc7,css:'#eadcc7',opacity:1,metalness:0};if((p.Kind||'').startsWith('hardware'))return{hex:0x252a31,css:'#252a31',opacity:1,metalness:.38};return{hex:0xcad4e0,css:'#cad4e0',opacity:1,metalness:p.Kind==='profile' ? .2 : 0};}
    function realisticAssemblyPartStyle(p){const name=((p&&p.Name)||'').toLowerCase();if(name.includes('kogelpotblad'))return{hex:0xf7f7f4,css:'#f7f7f4',opacity:ghostLexTop ? .32 : 1,metalness:.01};if(name.includes('stabilisatieplaat'))return{hex:0xf2f1ec,css:'#f2f1ec',opacity:1,metalness:.01};if(name.includes('hoekadapter')||name.includes('stellfußsockel'))return{hex:0xc8cdd1,css:'#c8cdd1',opacity:1,metalness:.72};if(name.includes('afdekkap')||name.includes('stelvoet')||name.includes('stelpoot')||name.includes('plunjer')||name.includes('eindstop'))return{hex:0x1c1f23,css:'#1c1f23',opacity:1,metalness:.18};if(name.includes('kogelpot'))return{hex:0xd9dfe3,css:'#d9dfe3',opacity:1,metalness:.96,roughness:.14};if(name.includes('hsr15'))return{hex:0x747d85,css:'#747d85',opacity:1,metalness:.68};if(name.includes('hte2'))return{hex:0x6f777e,css:'#6f777e',opacity:1,metalness:.42};if(name.includes('adapterplaat')||p.Kind==='profile')return{hex:0xb7bec4,css:'#b7bec4',opacity:1,metalness:.58};if(p.Kind==='sheet')return{hex:0xf2f1ec,css:'#f2f1ec',opacity:1,metalness:.01};if((p.Kind||'').startsWith('hardware'))return{hex:0x34393e,css:'#34393e',opacity:1,metalness:.5};return{hex:0xc4c9ce,css:'#c4c9ce',opacity:1,metalness:.36};}
    function drawGround(ctx,w,h,dpr){ctx.save();ctx.globalAlpha=.55;ctx.fillStyle='#dfe4eb';ctx.beginPath();ctx.ellipse(w/2,h*.78,w*.32,h*.045,0,0,Math.PI*2);ctx.fill();ctx.restore();}
    function renderOrthoPreview(){
      const parts=visibleAssemblyParts(),c=$('orthoCanvas');if(!c||!parts.length)return;const ctx=c.getContext('2d'),box=c.getBoundingClientRect(),dpr=window.devicePixelRatio||1,w=Math.max(520,Math.floor(box.width*dpr)),h=Math.max(320,Math.floor(box.height*dpr));if(c.width!==w||c.height!==h){c.width=w;c.height=h;}ctx.clearRect(0,0,w,h);const g=ctx.createLinearGradient(0,0,0,h);g.addColorStop(0,'#ffffff');g.addColorStop(1,'#f1f4f8');ctx.fillStyle=g;ctx.fillRect(0,0,w,h);
      const bounds=assemblyBounds(),pad=34*dpr,gap=24*dpr,frontW=(w-pad*2-gap)*.66,sideW=(w-pad*2-gap)-frontW,drawH=h-pad*2-36*dpr;
      drawOrthoSet(ctx,pad,pad+34*dpr,frontW,drawH,bounds,'front',dpr);drawOrthoSet(ctx,pad+frontW+gap,pad+34*dpr,sideW,drawH,bounds,'side',dpr);
      ctx.fillStyle='#1d1d1f';ctx.font=`${15*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText(lastQuote?lastQuote.ProductName+' assembly':'Assembly',pad,24*dpr);ctx.fillStyle='#6e6e73';ctx.font=`${11*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText('Zelfde assembly-data als 360 view',pad,40*dpr);
    }
    function drawOrthoSet(ctx,x,y,w,h,b,mode,dpr){
      const useX=mode==='front',spanX=useX?(b.maxX-b.minX):(b.maxZ-b.minZ),spanY=b.maxY-b.minY,scale=Math.min(w/Math.max(spanX,1),h/Math.max(spanY,1))*.92,ox=x+w/2,oy=y+h*.95;
      const viewCenter=useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2,drawLeft=ox+(0-spanX/2)*scale,drawRight=ox+(spanX/2)*scale,drawTop=oy-(spanY)*scale,drawBottom=oy;
      if(useX){ctx.save();ctx.translate(2*ox,0);ctx.scale(-1,1);}
      const list=[...visibleAssemblyParts()].sort((a,bp)=>(mode==='front'?bp.Zmm-a.Zmm:a.Xmm-bp.Xmm));list.forEach(p=>{const px=useX?p.Xmm:p.Zmm,psx=useX?p.SizeXmm:p.SizeZmm,py=p.Ymm,psy=p.SizeYmm,style=assemblyPartStyle(p);const rx=ox+(px-(useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2))*scale-psx*scale/2,ry=oy-(py-b.minY)*scale-psy*scale/2,rw=Math.max(1,psx*scale),rh=Math.max(1,psy*scale);ctx.globalAlpha=style.opacity;ctx.fillStyle=style.css;ctx.strokeStyle='rgba(52,64,84,.72)';ctx.lineWidth=1*dpr;if(useX&&(p.CornerRadiusMm||0)>0&&ctx.roundRect){ctx.beginPath();ctx.roundRect(rx,ry,rw,rh,Math.min((p.CornerRadiusMm||0)*scale,rw/2,rh/2));ctx.fill();ctx.stroke();}else{ctx.fillRect(rx,ry,rw,rh);ctx.strokeRect(rx,ry,rw,rh);}ctx.globalAlpha=1;(p.Pockets||[]).forEach(g=>{const gx=useX?g.Xmm:g.Zmm,gy=g.Ymm,gsx=useX?g.SizeXmm:g.SizeZmm,gsy=g.SizeYmm;if(gx+gsx/2<px-psx/2||gx-gsx/2>px+psx/2||gy+gsy/2<py-psy/2||gy-gsy/2>py+psy/2)return;ctx.fillStyle='rgba(80,72,60,.16)';ctx.strokeStyle='rgba(80,72,60,.52)';ctx.setLineDash([5*dpr,4*dpr]);ctx.fillRect(ox+(gx-(useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2))*scale-gsx*scale/2,oy-(gy-b.minY)*scale-gsy*scale/2,Math.max(1,gsx*scale),Math.max(1,gsy*scale));ctx.strokeRect(ox+(gx-(useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2))*scale-gsx*scale/2,oy-(gy-b.minY)*scale-gsy*scale/2,Math.max(1,gsx*scale),Math.max(1,gsy*scale));ctx.setLineDash([]);});(p.Holes||[]).forEach(hole=>{if(mode==='side'&&hole.DepthMm>0&&/^Zijwand/i.test(p.Name||''))return;const hx=useX?hole.Xmm:hole.Zmm,hy=hole.Ymm;if(hx<px-psx/2||hx>px+psx/2||hy<py-psy/2||hy>py+psy/2)return;ctx.beginPath();ctx.arc(ox+(hx-(useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2))*scale,oy-(hy-b.minY)*scale,Math.max(1.5*dpr,Math.max(hole.DiameterMm||0,hole.CountersinkDiameterMm||0)*scale*.45),0,Math.PI*2);ctx.fillStyle='#08090a';ctx.fill();});});
      if(useX)ctx.restore();
      ctx.strokeStyle='#98a2b3';ctx.lineWidth=1*dpr;ctx.strokeRect(x,y,w,h);ctx.fillStyle='#667085';ctx.font=`${11*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText(mode==='front'?'Vooraanzicht':'Zijaanzicht',x+8*dpr,y+17*dpr);
      drawDimension(ctx,drawLeft,drawBottom+18*dpr,drawRight,drawBottom+18*dpr,Math.round(spanX)+' mm '+(useX?'breed':'diep'),dpr,false);
      drawDimension(ctx,drawLeft-18*dpr,drawBottom,drawLeft-18*dpr,drawTop,Math.round(spanY)+' mm hoog',dpr,true);
    }
    function drawDimension(ctx,x1,y1,x2,y2,label,dpr,vertical){
      ctx.save();ctx.strokeStyle='#667085';ctx.fillStyle='#475467';ctx.lineWidth=1*dpr;ctx.beginPath();ctx.moveTo(x1,y1);ctx.lineTo(x2,y2);ctx.stroke();drawTick(ctx,x1,y1,vertical,dpr);drawTick(ctx,x2,y2,vertical,dpr);ctx.font=`${10*dpr}px -apple-system,Segoe UI,Arial`;if(vertical){ctx.translate(x1-8*dpr,(y1+y2)/2);ctx.rotate(-Math.PI/2);ctx.textAlign='center';ctx.fillText(label,0,0);}else{ctx.textAlign='center';ctx.fillText(label,(x1+x2)/2,y1+14*dpr);}ctx.restore();
    }
    function drawTick(ctx,x,y,vertical,dpr){ctx.beginPath();if(vertical){ctx.moveTo(x-4*dpr,y);ctx.lineTo(x+4*dpr,y);}else{ctx.moveTo(x,y-4*dpr);ctx.lineTo(x,y+4*dpr);}ctx.stroke();}
    function assemblyBounds(){let minX=1e9,maxX=-1e9,minY=1e9,maxY=-1e9,minZ=1e9,maxZ=-1e9;visibleAssemblyParts().forEach(p=>{minX=Math.min(minX,p.Xmm-p.SizeXmm/2);maxX=Math.max(maxX,p.Xmm+p.SizeXmm/2);minY=Math.min(minY,p.Ymm-p.SizeYmm/2);maxY=Math.max(maxY,p.Ymm+p.SizeYmm/2);minZ=Math.min(minZ,p.Zmm-p.SizeZmm/2);maxZ=Math.max(maxZ,p.Zmm+p.SizeZmm/2);});return{minX,maxX,minY,maxY,minZ,maxZ};}
    function loadThreeViewer(){
      if(threeApi)return Promise.resolve();
      if(threePromise)return threePromise;
      threePromise=Promise.all([
        import('/vendor/three/three.module.js'),
        import('/vendor/three/OrbitControls.js')
      ]).then(([THREE,controls])=>{threeApi={THREE,OrbitControls:controls.OrbitControls};document.body.classList.add('webglOn');}).catch(()=>{threeApi=null;});
      return threePromise;
    }
    function renderThreeAssembly(){
      const {THREE,OrbitControls}=threeApi,c=$('assemblyCanvas'),box=c.getBoundingClientRect(),w=Math.max(520,box.width),h=Math.max(420,box.height);
      if(!threeState){const renderer=new THREE.WebGLRenderer({canvas:c,antialias:true,alpha:true});renderer.setPixelRatio(Math.min(window.devicePixelRatio||1,2));renderer.setClearColor(0xf5f7fa,1);const scene=new THREE.Scene();const camera=new THREE.OrthographicCamera(-1,1,1,-1,.1,10000);const controls=new OrbitControls(camera,c);controls.enableDamping=true;controls.dampingFactor=.08;controls.enablePan=false;controls.enableZoom=true;controls.zoomSpeed=1.15;controls.minZoom=.06;controls.maxZoom=9;scene.add(new THREE.HemisphereLight(0xffffff,0x7d8996,2.35));const key=new THREE.DirectionalLight(0xffffff,3.1);key.position.set(3,5,4);scene.add(key);threeState={renderer,scene,camera,controls,group:new THREE.Group(),lastKey:'',lastW:0,lastH:0,forceFit:true};scene.add(threeState.group);controls.addEventListener('change',()=>threeState.renderer.render(threeState.scene,threeState.camera));c.addEventListener('wheel',e=>e.preventDefault(),{passive:false});}
      const parts=visibleAssemblyParts(),key=ghostLexTop+'|'+assemblyColorMode+'|'+JSON.stringify(parts.map(p=>[p.Name,p.Kind,p.Shape,p.Xmm,p.Ymm,p.Zmm,p.SizeXmm,p.SizeYmm,p.SizeZmm,p.CornerRadiusMm,p.BodyDiameterMm,p.FlangeDiameterMm,p.FlangeThicknessMm,p.FlangeRecessDepthMm,p.InsertionLengthMm,p.BallDiameterMm,p.WorkingHeightMm,(p.Pockets||[]).map(g=>[g.Name,g.Xmm,g.Ymm,g.Zmm,g.SizeXmm,g.SizeYmm,g.SizeZmm,g.Plane,g.IsThroughCutout]),(p.Holes||[]).map(h=>[h.Xmm,h.Ymm,h.Zmm,h.DiameterMm,h.DepthMm,h.Plane,h.Countersunk,h.CountersinkDiameterMm,h.CountersinkDepthMm])]));
      threeState.renderer.setSize(w,h,false);threeState.camera.left=-w/2;threeState.camera.right=w/2;threeState.camera.top=h/2;threeState.camera.bottom=-h/2;threeState.camera.updateProjectionMatrix();
      threeState.group.rotation.y=rotationDeg*Math.PI/180;
      const resized=Math.abs(threeState.lastW-w)>2||Math.abs(threeState.lastH-h)>2;if(threeState.lastKey!==key){threeState.group.clear();buildThreeParts(THREE,threeState.group,parts);threeState.lastKey=key;threeState.forceFit=true;}if(threeState.forceFit||resized){fitThreeCamera(THREE,threeState.camera,threeState.controls);threeState.forceFit=false;threeState.lastW=w;threeState.lastH=h;}
      threeState.controls.update();threeState.renderer.render(threeState.scene,threeState.camera);
    }
    function buildThreeParts(THREE,group,parts){
      parts.forEach(p=>{
        const door=isTransparentDoorPart(p),style=assemblyPartStyle(p);
        if(p.Shape==='ball-transfer'){addBallTransferUnit(THREE,group,p);return;}
        if(p.Shape==='leveling-caster'||p.Shape==='leveling-caster-leveled'){addLevelingCaster(THREE,group,p,p.Shape==='leveling-caster-leveled');return;}
        if(p.Shape==='footplate-m12'){addMachineBaseFootplate(THREE,group,p);return;}
        if(p.Shape==='pa40-hinge'){addPa40Hinge(THREE,group,p);return;}
        if((p.Shape||'').startsWith('crate-clip-')){addCrateSpringClip(THREE,group,p);return;}
        if(p.Shape==='black-hole-z'||p.Shape==='black-hole-x'||p.Shape==='black-hole-y'){addBlackConnectorHole(THREE,group,p);return;}
        const opacity=door?0.42:style.opacity;
        const transparent=opacity<1;
        const alignmentPin=p.Kind==='hardware-pin';
        const material=new THREE.MeshStandardMaterial({color:alignmentPin?0xd97706:style.hex,roughness:.55,metalness:style.metalness,transparent,opacity,depthTest:true,depthWrite:!transparent,side:THREE.DoubleSide});
        const through=(p.Pockets||[]).filter(g=>g.IsThroughCutout);
        const pockets=(p.Pockets||[]).filter(g=>!g.IsThroughCutout);
        const ballSeats=(p.Holes||[]).filter(h=>/^Kogelpot /i.test(h.Name||'')&&h.Countersunk&&h.CountersinkDiameterMm>h.DiameterMm&&h.CountersinkDepthMm>0);
        if(ballSeats.length){addCounterboredBallTransferSheet(THREE,group,p,material,ballSeats);return;}
        if(through.length){const geo=buildThroughCutoutGeometry(THREE,p,through);const mesh=new THREE.Mesh(geo,material);mesh.position.set(p.Xmm,p.Ymm,p.Zmm);group.add(mesh);const edges=new THREE.LineSegments(new THREE.EdgesGeometry(geo),new THREE.LineBasicMaterial({color:0x667085,transparent:true,opacity:door?0.26:0.38}));edges.position.copy(mesh.position);group.add(edges);addThreeHoles(THREE,group,p);return;}
        const realPocketSheet=p.Kind==='sheet'&&pockets.some(g=>g.Plane==='y'&&g.SizeYmm>1.5&&g.SizeXmm<p.SizeXmm*.2);
        const pocketable=p.Kind==='sheet'||p.Kind==='hardware-adapter';
        const realPocketZ=pocketable&&!realPocketSheet&&pockets.some(g=>g.Plane==='z'&&g.SizeZmm>1.5);
        const realPocketX=pocketable&&!realPocketSheet&&!realPocketZ&&pockets.some(g=>g.Plane==='x'&&g.SizeXmm>1.5);
        if(realPocketZ){addPocketedVerticalXPart(THREE,group,p,material);return;}
        if(realPocketX){addPocketedVerticalZPart(THREE,group,p,material);return;}
        const geo=realPocketSheet
          ?buildPocketedSheetGeometry(THREE,{...p,Pockets:pockets})
          :(p.Shape==='cylinder'
            ?new THREE.CylinderGeometry(p.SizeXmm/2,p.SizeZmm/2,p.SizeYmm,32)
            :((p.CornerRadiusMm||0)>.001
              ?buildRoundedPanelGeometry(THREE,p)
              :new THREE.BoxGeometry(p.SizeXmm,p.SizeYmm,p.SizeZmm)));
        const mesh=new THREE.Mesh(geo,material);mesh.position.set(p.Xmm,p.Ymm,p.Zmm);if(alignmentPin)mesh.renderOrder=19;group.add(mesh);
        const edges=new THREE.LineSegments(new THREE.EdgesGeometry(geo),new THREE.LineBasicMaterial({color:0x475467,transparent:true,opacity:transparent ? .3 : .62}));edges.position.copy(mesh.position);group.add(edges);
        addThreeHoles(THREE,group,p);
        addThreeCapsuleSlots(THREE,group,p);
        if(p.Kind==='profile')addProfileSlotLines(THREE,group,p);
      });
      const floorGeo=new THREE.CircleGeometry(900,64),floorMat=new THREE.MeshBasicMaterial({color:0xdfe4eb,transparent:true,opacity:.34});const floor=new THREE.Mesh(floorGeo,floorMat);floor.rotation.x=-Math.PI/2;floor.scale.set(1.4,.58,1);floor.position.y=-8;floor.userData.excludeFromFit=true;group.add(floor);
    }
    function addMachineBaseFootplate(THREE,group,p){
      const metal=new THREE.MeshStandardMaterial({color:assemblyColorMode==='technical'?0x7c8791:0xaeb4b8,roughness:.3,metalness:.82}),dark=new THREE.MeshBasicMaterial({color:0x111315,side:THREE.DoubleSide}),plate=new THREE.Mesh(new THREE.BoxGeometry(40,15,80),metal);plate.position.set(p.Xmm,p.Ymm,p.Zmm);group.add(plate);
      [-20,0,20].forEach((offset,index)=>{const radius=index===1?5:6.75,disc=new THREE.Mesh(new THREE.CircleGeometry(radius,28),dark);disc.rotation.x=-Math.PI/2;disc.position.set(p.Xmm,p.Ymm+7.56,p.Zmm+offset);disc.renderOrder=15;group.add(disc);});
      const edges=new THREE.LineSegments(new THREE.EdgesGeometry(plate.geometry),new THREE.LineBasicMaterial({color:0x4b5560,transparent:true,opacity:.65}));edges.position.copy(plate.position);group.add(edges);
    }
    function addPa40Hinge(THREE,group,p){
      const black=new THREE.MeshStandardMaterial({color:0x090b0d,roughness:.68,metalness:.08}),hole=new THREE.MeshBasicMaterial({color:0x000000,side:THREE.DoubleSide});
      const left=new THREE.Mesh(new THREE.BoxGeometry(26,50,8.5),black),right=new THREE.Mesh(new THREE.BoxGeometry(26,50,8.5),black);left.position.set(p.Xmm-18,p.Ymm,p.Zmm);right.position.set(p.Xmm+18,p.Ymm,p.Zmm);group.add(left,right);
      const barrel=new THREE.Mesh(new THREE.CylinderGeometry(6,6,50,24),black);barrel.position.set(p.Xmm,p.Ymm,p.Zmm);group.add(barrel);
      [-18,18].forEach(dx=>[-15,15].forEach(dy=>{const disc=new THREE.Mesh(new THREE.CircleGeometry(3.2,20),hole);disc.position.set(p.Xmm+dx,p.Ymm+dy,p.Zmm-4.3);disc.renderOrder=20;group.add(disc);}));
    }
    function addCrateSpringClip(THREE,group,p){
      const metal=new THREE.MeshStandardMaterial({color:assemblyColorMode==='technical'?0x111827:0x080a0d,roughness:.42,metalness:.72}),edge=new THREE.LineBasicMaterial({color:0x000000,transparent:true,opacity:.78});
      function box(sx,sy,sz,x,y,z){const geo=new THREE.BoxGeometry(sx,sy,sz),mesh=new THREE.Mesh(geo,metal);mesh.position.set(x,y,z);group.add(mesh);const lines=new THREE.LineSegments(new THREE.EdgesGeometry(geo),edge);lines.position.copy(mesh.position);group.add(lines);}
      const shape=p.Shape||'',signX=p.Xmm<0?-1:1,signZ=p.Zmm<0?-1:1,top=shape.endsWith('-top'),wallY=top?-1:1;
      if(shape==='crate-clip-corner'){
        box(2.2,48,36,p.Xmm+signX*1.1,p.Ymm,p.Zmm-signZ*18);
        box(56,48,2.2,p.Xmm-signX*28,p.Ymm,p.Zmm+signZ*1.1);
        box(5,15,10,p.Xmm-signX*1.8,p.Ymm,p.Zmm-signZ*31);
        box(10,15,5,p.Xmm-signX*50,p.Ymm,p.Zmm-signZ*1.8);
        return;
      }
      if(shape.includes('seam-x')){
        box(36,56,2.2,p.Xmm,p.Ymm+wallY*28,p.Zmm+signZ*1.1);
        box(36,2.2,36,p.Xmm,p.Ymm+(top?1.1:-1.1),p.Zmm-signZ*18);
        box(12,8,5,p.Xmm,p.Ymm+wallY*50,p.Zmm-signZ*1.8);
        box(12,5,8,p.Xmm,p.Ymm+(top?-1.8:1.8),p.Zmm-signZ*31);
        return;
      }
      box(2.2,56,36,p.Xmm+signX*1.1,p.Ymm+wallY*28,p.Zmm);
      box(36,2.2,36,p.Xmm-signX*18,p.Ymm+(top?1.1:-1.1),p.Zmm);
      box(5,8,12,p.Xmm-signX*1.8,p.Ymm+wallY*50,p.Zmm);
      box(8,5,12,p.Xmm-signX*31,p.Ymm+(top?-1.8:1.8),p.Zmm);
    }
    function addBlackConnectorHole(THREE,group,p){
      const depth=Math.max(1,p.Shape==='black-hole-x'?p.SizeXmm:(p.Shape==='black-hole-y'?p.SizeYmm:p.SizeZmm)),mat=new THREE.MeshBasicMaterial({color:0x000000,side:THREE.DoubleSide}),geo=new THREE.CylinderGeometry(3.5,3.5,depth,24),mesh=new THREE.Mesh(geo,mat);if(p.Shape==='black-hole-z')mesh.rotation.x=Math.PI/2;else if(p.Shape==='black-hole-x')mesh.rotation.z=Math.PI/2;mesh.position.set(p.Xmm,p.Ymm,p.Zmm);mesh.renderOrder=21;group.add(mesh);
    }
    function addLevelingCaster(THREE,group,p,leveled){
      const technical=assemblyColorMode==='technical',housingMat=new THREE.MeshStandardMaterial({color:technical?0xb7c1ca:0xe4e0d2,roughness:.38,metalness:.38}),steelMat=new THREE.MeshStandardMaterial({color:technical?0x687683:0xaeb6bc,roughness:.28,metalness:.82}),wheelMat=new THREE.MeshStandardMaterial({color:0x171a1d,roughness:.72,metalness:.05}),rubberMat=new THREE.MeshStandardMaterial({color:0x25282b,roughness:.92,metalness:0}),handleMat=new THREE.MeshStandardMaterial({color:0xd84b24,roughness:.55,metalness:.05}),bottom=p.Ymm-p.SizeYmm/2;
      const lift=leveled?10:0,wheel=new THREE.Mesh(new THREE.CylinderGeometry(25,25,25,32),wheelMat);wheel.rotation.x=Math.PI/2;wheel.position.set(p.Xmm-36,bottom+25+lift,p.Zmm);group.add(wheel);
      const body=new THREE.Mesh(new THREE.CylinderGeometry(31,27,49,8),housingMat);body.position.set(p.Xmm,bottom+48+lift,p.Zmm);group.add(body);
      const pad=new THREE.Mesh(new THREE.CylinderGeometry(24,24,13,32),rubberMat);pad.position.set(p.Xmm,bottom+6.5,p.Zmm);group.add(pad);
      const handle=new THREE.Mesh(new THREE.CylinderGeometry(21,21,9,12),handleMat);handle.position.set(p.Xmm,bottom+67+lift,p.Zmm);group.add(handle);
      const swivel=new THREE.Mesh(new THREE.CylinderGeometry(27,27,8,32),steelMat);swivel.position.set(p.Xmm,bottom+77+lift,p.Zmm);group.add(swivel);
      const stem=new THREE.Mesh(new THREE.CylinderGeometry(6,6,30,20),steelMat);stem.position.set(p.Xmm,bottom+91+lift,p.Zmm);group.add(stem);
    }
    function roundedRectangleShape(THREE,uMin,uMax,vMin,vMax,radius){
      const r=Math.max(0,Math.min(radius||0,(uMax-uMin)/2,(vMax-vMin)/2)),shape=new THREE.Shape();
      if(r<.001){shape.moveTo(uMin,vMin);shape.lineTo(uMax,vMin);shape.lineTo(uMax,vMax);shape.lineTo(uMin,vMax);shape.closePath();return shape;}
      shape.moveTo(uMin+r,vMin);shape.lineTo(uMax-r,vMin);shape.absarc(uMax-r,vMin+r,r,-Math.PI/2,0,false);
      shape.lineTo(uMax,vMax-r);shape.absarc(uMax-r,vMax-r,r,0,Math.PI/2,false);
      shape.lineTo(uMin+r,vMax);shape.absarc(uMin+r,vMax-r,r,Math.PI/2,Math.PI,false);
      shape.lineTo(uMin,vMin+r);shape.absarc(uMin+r,vMin+r,r,Math.PI,Math.PI*1.5,false);shape.closePath();return shape;
    }
    function buildRoundedPanelGeometry(THREE,p){
      const shape=roundedRectangleShape(THREE,-p.SizeXmm/2,p.SizeXmm/2,-p.SizeYmm/2,p.SizeYmm/2,p.CornerRadiusMm||0);
      const geo=new THREE.ExtrudeGeometry(shape,{depth:p.SizeZmm,steps:1,curveSegments:12,bevelEnabled:false});geo.translate(0,0,-p.SizeZmm/2);geo.computeVertexNormals();return geo;
    }
    function addCounterboredBallTransferSheet(THREE,group,p,material,holes){
      const recess=Math.min(p.SizeYmm-.2,Math.max(.2,holes[0].CountersinkDepthMm||0)),bottomHeight=p.SizeYmm-recess,bottomY=p.Ymm-p.SizeYmm/2,edgeMat=new THREE.LineBasicMaterial({color:0x667085,transparent:true,opacity:.42});
      function layer(height,y,diameterOf){
        const sx=p.SizeXmm/2,sz=p.SizeZmm/2,shape=new THREE.Shape();shape.moveTo(-sx,-sz);shape.lineTo(sx,-sz);shape.lineTo(sx,sz);shape.lineTo(-sx,sz);shape.closePath();
        holes.forEach(h=>{const radius=Math.max(.5,diameterOf(h)/2),path=new THREE.Path();path.absarc(h.Xmm-p.Xmm,-(h.Zmm-p.Zmm),radius,0,Math.PI*2,true);shape.holes.push(path);});
        const geo=new THREE.ExtrudeGeometry(shape,{depth:height,steps:1,curveSegments:28,bevelEnabled:false}),mesh=new THREE.Mesh(geo,material);mesh.rotation.x=-Math.PI/2;mesh.position.set(p.Xmm,y,p.Zmm);group.add(mesh);const edges=new THREE.LineSegments(new THREE.EdgesGeometry(geo),edgeMat);edges.rotation.copy(mesh.rotation);edges.position.copy(mesh.position);group.add(edges);
      }
      layer(bottomHeight,bottomY,h=>h.DiameterMm);
      layer(recess,bottomY+bottomHeight,h=>h.CountersinkDiameterMm);
    }
    function addBallTransferUnit(THREE,group,p){
      const bodyD=Math.max(2,p.BodyDiameterMm||p.SizeXmm),flangeD=Math.max(bodyD,p.FlangeDiameterMm||p.SizeXmm),insert=Math.max(2,p.InsertionLengthMm||Math.max(2,p.SizeYmm-(p.WorkingHeightMm||0))),ballD=Math.max(2,p.BallDiameterMm||bodyD*.63),work=Math.max(.5,p.WorkingHeightMm||2),recess=Math.max(0,p.FlangeRecessDepthMm||0),flangeHeight=Math.max(.6,p.FlangeThicknessMm||Math.max(1,recess)),surfaceY=p.Ymm+(insert+recess-work)/2,flangeSeatY=surfaceY-recess,technical=assemblyColorMode==='technical';
      const housingMat=new THREE.MeshStandardMaterial({color:technical?0x4f5b66:0xaeb6bd,roughness:technical?.38:.22,metalness:technical?.68:.9});
      const flangeMat=new THREE.MeshStandardMaterial({color:technical?0x71808d:0xcbd1d6,roughness:technical?.3:.14,metalness:technical?.76:.95});
      const ballMat=new THREE.MeshStandardMaterial({color:technical?0xe7eef3:0xf1f4f6,roughness:technical?.14:.06,metalness:technical?.52:.98,emissive:technical?0x111820:0x000000,emissiveIntensity:technical?.16:0});
      const edgeMat=new THREE.LineBasicMaterial({color:0x39434c,transparent:true,opacity:.58});
      function cylinder(d,h,y,material,segments){const geo=new THREE.CylinderGeometry(d/2,d/2,h,segments||24),mesh=new THREE.Mesh(geo,material);mesh.position.set(p.Xmm,y,p.Zmm);group.add(mesh);const edges=new THREE.LineSegments(new THREE.EdgesGeometry(geo),edgeMat);edges.position.copy(mesh.position);group.add(edges);}
      cylinder(bodyD,insert,flangeSeatY-insert/2,housingMat,24);
      cylinder(flangeD,flangeHeight,surfaceY-flangeHeight/2,flangeMat,32);
      const ballRadius=ballD/2,capHeight=Math.min(work,ballRadius*.95),capTheta=Math.acos(Math.max(-1,Math.min(1,(ballRadius-capHeight)/ballRadius))),ballGeo=new THREE.SphereGeometry(ballRadius,36,16,0,Math.PI*2,0,capTheta),ball=new THREE.Mesh(ballGeo,ballMat);ball.position.set(p.Xmm,surfaceY+work-ballRadius,p.Zmm);group.add(ball);
      const ringTube=Math.max(.55,Math.min(.8,ballRadius*.08)),ringGeo=new THREE.TorusGeometry(ballRadius*.78,ringTube,10,36),ring=new THREE.Mesh(ringGeo,flangeMat);ring.rotation.x=Math.PI/2;ring.position.set(p.Xmm,surfaceY-ringTube*.6,p.Zmm);group.add(ring);
    }
    function isDoorPanelPart(p){const name=((p&&p.Name)||'').toLowerCase();return name.startsWith('draaideur ')||/^schuifdeur u\d+/.test(name);}
    function isTransparentDoorPart(p){return isDoorPanelPart(p);}
    function addThreeHoles(THREE,group,p){
      (p.Holes||[]).filter(h=>!/^Kogelpot /i.test(h.Name||'')).forEach(h=>{const visibleDiameter=Math.max(h.DiameterMm||0,h.CountersinkDiameterMm||0),hg=new THREE.CircleGeometry(Math.max(2.25,visibleDiameter/2),28),hm=new THREE.MeshBasicMaterial({color:0x08090a,transparent:true,opacity:.94,side:THREE.DoubleSide,depthTest:true,depthWrite:false,polygonOffset:true,polygonOffsetFactor:-4,polygonOffsetUnits:-4}),hole=new THREE.Mesh(hg,hm);hole.position.set(h.Xmm,h.Ymm,h.Zmm);if(h.Plane==='x')hole.rotation.y=Math.PI/2;else if(h.Plane==='y')hole.rotation.x=-Math.PI/2;hole.renderOrder=12;group.add(hole);});
    }
    function addThreeCapsuleSlots(THREE,group,p){
      (p.Pockets||[]).filter(slot=>slot.Shape==='capsule').forEach(slot=>{
        const length=Math.max(slot.SizeXmm||0,slot.SizeZmm||0),width=Math.max(2,Math.min(slot.SizeXmm||0,slot.SizeZmm||0)),radius=width/2,straight=Math.max(0,length/2-radius),shape=new THREE.Shape();
        shape.moveTo(-straight,-radius);shape.lineTo(straight,-radius);shape.absarc(straight,0,radius,-Math.PI/2,Math.PI/2,false);shape.lineTo(-straight,radius);shape.absarc(-straight,0,radius,Math.PI/2,-Math.PI/2,false);
        const geometry=new THREE.ShapeGeometry(shape,24),material=new THREE.MeshBasicMaterial({color:0x08090a,transparent:true,opacity:.96,side:THREE.DoubleSide,depthTest:true,depthWrite:false,polygonOffset:true,polygonOffsetFactor:-4,polygonOffsetUnits:-4});
        [-1,1].forEach(face=>{const marker=new THREE.Mesh(geometry,material);marker.position.set(slot.Xmm,p.Ymm+face*(p.SizeYmm/2+.24),slot.Zmm);marker.rotation.x=-Math.PI/2;marker.renderOrder=13;group.add(marker);});
      });
    }
    function addProfileSlotLines(THREE,group,p){
      const sx=p.SizeXmm/2,sy=p.SizeYmm/2,sz=p.SizeZmm/2,longAxis=p.SizeXmm>=p.SizeYmm&&p.SizeXmm>=p.SizeZmm?'x':(p.SizeYmm>=p.SizeZmm?'y':'z'),cross=Math.max(1,Math.min(...[p.SizeXmm,p.SizeYmm,p.SizeZmm].filter((_,i)=>['x','y','z'][i]!==longAxis))),gap=Math.max(2.2,Math.min(4,cross*.08)),e=.32,points=[];
      function seg(a,b){points.push(a,b);}
      function slotLines(faceWidth){const centers=faceWidth>=60?[-faceWidth/4,faceWidth/4]:[0];return centers.flatMap(center=>[center-gap,center+gap]);}
      if(longAxis==='x'){
        [-1,1].forEach(face=>slotLines(p.SizeZmm).forEach(o=>seg(new THREE.Vector3(p.Xmm-sx,p.Ymm+face*(sy+e),p.Zmm+o),new THREE.Vector3(p.Xmm+sx,p.Ymm+face*(sy+e),p.Zmm+o))));
        [-1,1].forEach(face=>slotLines(p.SizeYmm).forEach(o=>seg(new THREE.Vector3(p.Xmm-sx,p.Ymm+o,p.Zmm+face*(sz+e)),new THREE.Vector3(p.Xmm+sx,p.Ymm+o,p.Zmm+face*(sz+e)))));
      }else if(longAxis==='z'){
        [-1,1].forEach(face=>slotLines(p.SizeXmm).forEach(o=>seg(new THREE.Vector3(p.Xmm+o,p.Ymm+face*(sy+e),p.Zmm-sz),new THREE.Vector3(p.Xmm+o,p.Ymm+face*(sy+e),p.Zmm+sz))));
        [-1,1].forEach(face=>slotLines(p.SizeYmm).forEach(o=>seg(new THREE.Vector3(p.Xmm+face*(sx+e),p.Ymm+o,p.Zmm-sz),new THREE.Vector3(p.Xmm+face*(sx+e),p.Ymm+o,p.Zmm+sz))));
      }else{
        [-1,1].forEach(face=>slotLines(p.SizeZmm).forEach(o=>seg(new THREE.Vector3(p.Xmm+face*(sx+e),p.Ymm-sy,p.Zmm+o),new THREE.Vector3(p.Xmm+face*(sx+e),p.Ymm+sy,p.Zmm+o))));
        [-1,1].forEach(face=>slotLines(p.SizeXmm).forEach(o=>seg(new THREE.Vector3(p.Xmm+o,p.Ymm-sy,p.Zmm+face*(sz+e)),new THREE.Vector3(p.Xmm+o,p.Ymm+sy,p.Zmm+face*(sz+e)))));
      }
      const geometry=new THREE.BufferGeometry().setFromPoints(points),material=new THREE.LineBasicMaterial({color:assemblyColorMode==='technical'?0x3f5365:0x626b73,transparent:true,opacity:.82,depthTest:true,depthWrite:false}),lines=new THREE.LineSegments(geometry,material);lines.renderOrder=8;group.add(lines);
    }
    function buildThroughCutoutGeometry(THREE,p,cutouts){
      const plane=(cutouts[0]&&cutouts[0].Plane)||'y';
      let uMin,uMax,vMin,vMax,thick;
      if(plane==='x'){uMin=-p.SizeZmm/2;uMax=p.SizeZmm/2;vMin=-p.SizeYmm/2;vMax=p.SizeYmm/2;thick=p.SizeXmm;}
      else if(plane==='z'){uMin=-p.SizeXmm/2;uMax=p.SizeXmm/2;vMin=-p.SizeYmm/2;vMax=p.SizeYmm/2;thick=p.SizeZmm;}
      else{uMin=-p.SizeXmm/2;uMax=p.SizeXmm/2;vMin=-p.SizeZmm/2;vMax=p.SizeZmm/2;thick=p.SizeYmm;}
      const localCutouts=cutouts.filter(g=>g.Plane===plane).map(g=>{
        let u,uw,v,vh;
        if(plane==='x'){u=g.Zmm-p.Zmm;uw=g.SizeZmm;v=g.Ymm-p.Ymm;vh=g.SizeYmm;}
        else if(plane==='z'){u=g.Xmm-p.Xmm;uw=g.SizeXmm;v=g.Ymm-p.Ymm;vh=g.SizeYmm;}
        else{u=g.Xmm-p.Xmm;uw=g.SizeXmm;v=g.Zmm-p.Zmm;vh=g.SizeZmm;}
        const u0=Math.max(uMin,u-uw/2),u1=Math.min(uMax,u+uw/2),v0=Math.max(vMin,v-vh/2),v1=Math.min(vMax,v+vh/2);
        return {...g,u0,u1,v0,v1,isHandle:(g.Name||'').toLowerCase().includes('handgreep')};
      }).filter(g=>g.u1-g.u0>=.2&&g.v1-g.v0>=.2);
      const edgeCutouts=plane==='y'?localCutouts.filter(g=>!g.isHandle&&g.v0<=vMin+.05&&(g.u0<=uMin+.05||g.u1>=uMax-.05)):[];
      const leftFront=edgeCutouts.find(g=>g.u0<=uMin+.05),rightFront=edgeCutouts.find(g=>g.u1>=uMax-.05);
      let shape;
      if((p.Outline||[]).length>=3){shape=new THREE.Shape((p.Outline||[]).map(o=>new THREE.Vector2(o.Umm,o.Vmm)));}
      else if(plane==='z'&&!leftFront&&!rightFront&&(p.CornerRadiusMm||0)>.001){shape=roundedRectangleShape(THREE,uMin,uMax,vMin,vMax,p.CornerRadiusMm||0);}
      else{
        const outline=[];
        outline.push(new THREE.Vector2(leftFront?leftFront.u1:uMin,vMin));
        outline.push(new THREE.Vector2(rightFront?rightFront.u0:uMax,vMin));
        if(rightFront){outline.push(new THREE.Vector2(rightFront.u0,rightFront.v1),new THREE.Vector2(uMax,rightFront.v1));}
        outline.push(new THREE.Vector2(uMax,vMax),new THREE.Vector2(uMin,vMax));
        if(leftFront){outline.push(new THREE.Vector2(uMin,leftFront.v1),new THREE.Vector2(leftFront.u1,leftFront.v1));}
        shape=new THREE.Shape(outline);
      }
      const handleCutouts=localCutouts.filter(g=>g.isHandle);
      if(handleCutouts.length>=2){
        const u0=Math.min(...handleCutouts.map(g=>g.u0)),u1=Math.max(...handleCutouts.map(g=>g.u1)),v0=Math.min(...handleCutouts.map(g=>g.v0)),v1=Math.max(...handleCutouts.map(g=>g.v1));
        const r=Math.min((u1-u0)/2,(v1-v0)/2),hole=new THREE.Path();
        if(u1-u0>=v1-v0){
          const cy=(v0+v1)/2,cx0=u0+r,cx1=u1-r;
          hole.moveTo(cx0,v0);hole.lineTo(cx1,v0);hole.absarc(cx1,cy,r,-Math.PI/2,Math.PI/2,false);hole.lineTo(cx0,v1);hole.absarc(cx0,cy,r,Math.PI/2,-Math.PI/2,false);
        }else{
          const cx=(u0+u1)/2,cy0=v0+r,cy1=v1-r;
          hole.moveTo(u1,cy0);hole.lineTo(u1,cy1);hole.absarc(cx,cy1,r,0,Math.PI,false);hole.lineTo(u0,cy0);hole.absarc(cx,cy0,r,Math.PI,0,false);
        }
        shape.holes.push(hole);
      }
      localCutouts.filter(g=>!edgeCutouts.includes(g)&&(!g.isHandle||handleCutouts.length<2)).forEach(g=>{
        const hole=new THREE.Path();
        if(g.Shape==='capsule'){
          const width=g.u1-g.u0,height=g.v1-g.v0,r=Math.min(width,height)/2;
          if(width>=height){const cy=(g.v0+g.v1)/2,cx0=g.u0+r,cx1=g.u1-r;hole.moveTo(cx0,g.v0);hole.lineTo(cx1,g.v0);hole.absarc(cx1,cy,r,-Math.PI/2,Math.PI/2,false);hole.lineTo(cx0,g.v1);hole.absarc(cx0,cy,r,Math.PI/2,-Math.PI/2,false);}
          else{const cx=(g.u0+g.u1)/2,cy0=g.v0+r,cy1=g.v1-r;hole.moveTo(g.u1,cy0);hole.lineTo(g.u1,cy1);hole.absarc(cx,cy1,r,0,Math.PI,false);hole.lineTo(g.u0,cy0);hole.absarc(cx,cy0,r,Math.PI,0,false);}
        }else{hole.moveTo(g.u0,g.v0);hole.lineTo(g.u0,g.v1);hole.lineTo(g.u1,g.v1);hole.lineTo(g.u1,g.v0);hole.lineTo(g.u0,g.v0);}
        shape.holes.push(hole);
      });
      const geo=new THREE.ExtrudeGeometry(shape,{depth:thick,bevelEnabled:false});
      geo.translate(0,0,-thick/2);
      const pos=geo.attributes.position;
      for(let i=0;i<pos.count;i++){
        const u=pos.getX(i),v=pos.getY(i),w=pos.getZ(i);
        if(plane==='x'){pos.setXYZ(i,w,v,u);}
        else if(plane==='z'){pos.setXYZ(i,u,v,w);}
        else{pos.setXYZ(i,u,w,v);}
      }
      pos.needsUpdate=true;geo.computeVertexNormals();return geo;
    }
    function rectCells(THREE,p,pockets,uSize,vSize,uName,vName,uSizeName,vSizeName){
      const us=[-uSize/2,uSize/2],vs=[-vSize/2,vSize/2];
      const rects=pockets.map(g=>{const uw=g[uSizeName],vh=g[vSizeName],u=g[uName]-p[uName]-uw/2,v=g[vName]-p[vName]-vh/2;const r={u0:Math.max(-uSize/2,u),u1:Math.min(uSize/2,u+uw),v0:Math.max(-vSize/2,v),v1:Math.min(vSize/2,v+vh)};us.push(r.u0,r.u1);vs.push(r.v0,r.v1);return r;});
      us.sort((a,b)=>a-b);vs.sort((a,b)=>a-b);const cells=[];
      for(let i=0;i<us.length-1;i++)for(let j=0;j<vs.length-1;j++){const u0=us[i],u1=us[i+1],v0=vs[j],v1=vs[j+1],cu=(u0+u1)/2,cv=(v0+v1)/2;if(u1-u0<.2||v1-v0<.2)continue;if(rects.some(r=>cu>=r.u0&&cu<=r.u1&&cv>=r.v0&&cv<=r.v1))continue;cells.push({u0,u1,v0,v1});}
      return cells;
    }
    function addPocketedVerticalXPart(THREE,group,p,material){
      const pockets=(p.Pockets||[]).filter(g=>g.Plane==='z'&&g.SizeZmm>0),depth=Math.min(p.SizeZmm*.8,Math.max(...pockets.map(g=>g.SizeZmm))),insidePlus=pockets.reduce((n,g)=>n+(g.Zmm>p.Zmm?1:-1),0)>=0;
      const mapped=pockets.map(g=>({u0:g.Xmm-p.Xmm-g.SizeXmm/2,u1:g.Xmm-p.Xmm+g.SizeXmm/2,v0:g.Ymm-p.Ymm-g.SizeYmm/2,v1:g.Ymm-p.Ymm+g.SizeYmm/2,depth:g.SizeZmm}));
      const geo=buildRecessedBoxGeometry(THREE,p,'z',insidePlus?1:-1,mapped);
      const mesh=new THREE.Mesh(geo,material);mesh.position.set(p.Xmm,p.Ymm,p.Zmm);group.add(mesh);
      const edges=new THREE.LineSegments(new THREE.EdgesGeometry(geo),new THREE.LineBasicMaterial({color:0x667085,transparent:true,opacity:.48}));edges.position.copy(mesh.position);group.add(edges);
      addThreeHoles(THREE,group,p);
    }
    function addPocketedVerticalZPart(THREE,group,p,material){
      const pockets=(p.Pockets||[]).filter(g=>g.Plane==='x'&&g.SizeXmm>0),depth=Math.min(p.SizeXmm*.8,Math.max(...pockets.map(g=>g.SizeXmm))),insidePlus=pockets.reduce((n,g)=>n+(g.Xmm>p.Xmm?1:-1),0)>=0;
      const mapped=pockets.map(g=>({u0:g.Zmm-p.Zmm-g.SizeZmm/2,u1:g.Zmm-p.Zmm+g.SizeZmm/2,v0:g.Ymm-p.Ymm-g.SizeYmm/2,v1:g.Ymm-p.Ymm+g.SizeYmm/2,depth:g.SizeXmm}));
      const geo=buildRecessedBoxGeometry(THREE,p,'x',insidePlus?1:-1,mapped);
      const mesh=new THREE.Mesh(geo,material);mesh.position.set(p.Xmm,p.Ymm,p.Zmm);group.add(mesh);
      const edges=new THREE.LineSegments(new THREE.EdgesGeometry(geo),new THREE.LineBasicMaterial({color:0x667085,transparent:true,opacity:.48}));edges.position.copy(mesh.position);group.add(edges);
      addThreeHoles(THREE,group,p);
    }
    function isDrawerPart(p){const n=p&&p.Name?p.Name:'';return n.startsWith('Lade')||n.startsWith('Bovenlade');}
    function buildRecessedBoxGeometry(THREE,p,axis,sign,pockets){
      const verts=[],idx=[];function addV(x,y,z){verts.push(x,y,z);return verts.length/3-1;}function quad(a,b,c,d){const ia=addV(...a),ib=addV(...b),ic=addV(...c),id=addV(...d);idx.push(ia,ib,ic,ia,ic,id);}
      const sx=p.SizeXmm/2,sy=p.SizeYmm/2,sz=p.SizeZmm/2;
      function xyz(u,v,w){return axis==='z'?[u,v,w]:[w,v,u];}
      const uMin=axis==='z'?-sx:-sz,uMax=axis==='z'?sx:sz,vMin=-sy,vMax=sy,wMin=axis==='z'?-sz:-sx,wMax=axis==='z'?sz:sx,wFace=sign>0?wMax:wMin;
      const ranges=pockets.map(r=>({u0:Math.max(uMin,r.u0),u1:Math.min(uMax,r.u1),v0:Math.max(vMin,r.v0),v1:Math.min(vMax,r.v1),d:Math.min((wMax-wMin)*.85,Math.max(.4,r.depth))})).filter(r=>r.u1>r.u0&&r.v1>r.v0);
      const wOpp=sign>0?wMin:wMax;
      quad(xyz(uMin,vMin,wOpp),xyz(uMax,vMin,wOpp),xyz(uMax,vMax,wOpp),xyz(uMin,vMax,wOpp));
      const us=[uMin,uMax],vs=[vMin,vMax];ranges.forEach(r=>{us.push(r.u0,r.u1);vs.push(r.v0,r.v1);});us.sort((a,b)=>a-b);vs.sort((a,b)=>a-b);
      const depth=[];function cellDepth(u0,u1,v0,v1){const cu=(u0+u1)/2,cv=(v0+v1)/2;let d=0;ranges.forEach(r=>{if(cu>=r.u0&&cu<=r.u1&&cv>=r.v0&&cv<=r.v1)d=Math.max(d,r.d);});return d;}
      for(let i=0;i<us.length-1;i++){depth[i]=[];for(let j=0;j<vs.length-1;j++){const u0=us[i],u1=us[i+1],v0=vs[j],v1=vs[j+1];if(u1-u0<.2||v1-v0<.2){depth[i][j]=0;continue;}const d=cellDepth(u0,u1,v0,v1);depth[i][j]=d;const w=d>0?wFace-sign*d:wFace;quad(xyz(u0,v0,w),xyz(u1,v0,w),xyz(u1,v1,w),xyz(u0,v1,w));}}
      function addOuterU(i,u){for(let j=0;j<vs.length-1;j++){const v0=vs[j],v1=vs[j+1],d=depth[i][j]||0,w= wFace-sign*d;quad(xyz(u,v0,wOpp),xyz(u,v1,wOpp),xyz(u,v1,w),xyz(u,v0,w));}}
      function addOuterV(j,v){for(let i=0;i<us.length-1;i++){const u0=us[i],u1=us[i+1],d=depth[i][j]||0,w= wFace-sign*d;quad(xyz(u0,v,wOpp),xyz(u1,v,wOpp),xyz(u1,v,w),xyz(u0,v,w));}}
      addOuterU(0,uMin);addOuterU(us.length-2,uMax);addOuterV(0,vMin);addOuterV(vs.length-2,vMax);
      function addWallU(i,j,dA,dB){if(Math.abs(dA-dB)<.2)return;const u=us[i],v0=vs[j],v1=vs[j+1],wA=wFace-sign*dA,wB=wFace-sign*dB;quad(xyz(u,v0,wA),xyz(u,v1,wA),xyz(u,v1,wB),xyz(u,v0,wB));}
      function addWallV(i,j,dA,dB){if(Math.abs(dA-dB)<.2)return;const v=vs[j],u0=us[i],u1=us[i+1],wA=wFace-sign*dA,wB=wFace-sign*dB;quad(xyz(u0,v,wA),xyz(u1,v,wA),xyz(u1,v,wB),xyz(u0,v,wB));}
      for(let i=1;i<us.length-1;i++)for(let j=0;j<vs.length-1;j++)addWallU(i,j,depth[i-1][j],depth[i][j]);
      for(let i=0;i<us.length-1;i++)for(let j=1;j<vs.length-1;j++)addWallV(i,j,depth[i][j-1],depth[i][j]);
      const geo=new THREE.BufferGeometry();geo.setAttribute('position',new THREE.Float32BufferAttribute(verts,3));geo.setIndex(idx);geo.computeVertexNormals();return geo;
    }
    function buildPocketedSheetGeometry(THREE,p){
      const x0=-p.SizeXmm/2,x1=p.SizeXmm/2,y0=-p.SizeYmm/2,y1=p.SizeYmm/2;
      const ranges=(p.Pockets||[]).filter(g=>g.Plane==='y'&&g.SizeYmm>0&&g.SizeXmm>0).map(g=>({a:Math.max(x0,g.Xmm-p.Xmm-g.SizeXmm/2),b:Math.min(x1,g.Xmm-p.Xmm+g.SizeXmm/2),d:Math.min(p.SizeYmm*.75,Math.max(.5,g.SizeYmm))})).filter(r=>r.b>r.a).sort((a,b)=>a.a-b.a);
      const merged=[];ranges.forEach(r=>{const last=merged[merged.length-1];if(!last||r.a>last.b){merged.push({...r});}else{last.b=Math.max(last.b,r.b);last.d=Math.max(last.d,r.d);}});
      const fromTop=(p.Pockets||[]).filter(g=>g.Plane==='y').reduce((score,g)=>score+(g.Ymm>=p.Ymm?1:-1),0)>=0;
      const pts=[];
      if(fromTop){
        pts.push(new THREE.Vector2(x0,y0),new THREE.Vector2(x1,y0),new THREE.Vector2(x1,y1));
        [...merged].reverse().forEach(r=>{pts.push(new THREE.Vector2(r.b,y1),new THREE.Vector2(r.b,y1-r.d),new THREE.Vector2(r.a,y1-r.d),new THREE.Vector2(r.a,y1));});
        pts.push(new THREE.Vector2(x0,y1));
      }else{
        pts.push(new THREE.Vector2(x0,y0));
        merged.forEach(r=>{pts.push(new THREE.Vector2(r.a,y0),new THREE.Vector2(r.a,y0+r.d),new THREE.Vector2(r.b,y0+r.d),new THREE.Vector2(r.b,y0));});
        pts.push(new THREE.Vector2(x1,y0),new THREE.Vector2(x1,y1),new THREE.Vector2(x0,y1));
      }
      const shape=new THREE.Shape(pts),geo=new THREE.ExtrudeGeometry(shape,{depth:p.SizeZmm,bevelEnabled:false});
      geo.translate(0,0,-p.SizeZmm/2);geo.computeVertexNormals();return geo;
    }
    function fitThreeCamera(THREE,camera,controls){
      threeState.group.updateMatrixWorld(true);const box=new THREE.Box3();threeState.group.children.filter(x=>!x.userData.excludeFromFit).forEach(x=>box.expandByObject(x));if(box.isEmpty())return;const size=box.getSize(new THREE.Vector3()),center=box.getCenter(new THREE.Vector3()),span=Math.max(size.x,size.y,size.z,1),w=threeState.renderer.domElement.clientWidth||520,h=threeState.renderer.domElement.clientHeight||420;camera.up.set(0,1,0);if(assemblyViewMode==='front'||assemblyViewMode==='side')camera.position.set(center.x,center.y,center.z+span*2);else if(assemblyViewMode==='underside')camera.position.set(center.x+span*.82,center.y-span*.38,center.z+span);else camera.position.set(center.x+span*.88,center.y+span*.44,center.z+span);camera.lookAt(center);const diagonal=assemblyViewMode==='iso'||assemblyViewMode==='underside',widthFactor=diagonal ? .70 : .84,heightFactor=diagonal ? .74 : .82;camera.zoom=Math.min(3,Math.max(.06,Math.min(w*widthFactor/Math.max(size.x,size.z,1),h*heightFactor/Math.max(size.y,1))));camera.updateProjectionMatrix();controls.target.copy(center);controls.update();
    }
    const syncBaseProductUi=syncProductUi;
    syncProductUi=function(){syncBaseProductUi();const product=$('product').value,isMachineBase=product==='machinebasis',isRobotCell=product==='robotcel',isShippingBox=product==='shipping_box';document.body.classList.toggle('isMachineBase',isMachineBase);document.body.classList.toggle('isRobotCell',isRobotCell);document.body.classList.toggle('isShippingBox',isShippingBox);if(isMachineBase){$('productName').textContent='Parametrische machinebasis';$('widthLabel').textContent='Buitenbreedte frame mm';$('depthLabel').textContent='Buitendiepte frame mm';$('heightLabel').textContent='Totale hoogte vanaf vloer mm';[['widthMm',1000,6000],['depthMm',600,2000],['heightMm',1000,2300]].forEach(x=>{const el=$(x[0]);el.min=x[1];el.max=x[2];el.step=10;});$('generateBtn').textContent='Genereer machineframe';}else if(isRobotCell){$('productName').textContent='Robot cel';$('widthLabel').textContent='Buitenbreedte frame mm';$('depthLabel').textContent='Buitendiepte frame mm';$('heightLabel').textContent='Werkbladhoogte mm';[['widthMm',600,6000],['depthMm',500,2000],['heightMm',650,1200]].forEach(x=>{const el=$(x[0]);el.min=x[1];el.max=x[2];el.step=10;});$('generateBtn').textContent='Genereer robot cel';}else if(isShippingBox){$('productName').textContent='Shipping box / clipkist';$('widthLabel').textContent='Binnenlengte mm';$('depthLabel').textContent='Binnenbreedte mm';$('heightLabel').textContent='Binnenhoogte mm';[['widthMm',200,6000],['depthMm',200,3000],['heightMm',200,3000]].forEach(x=>{const el=$(x[0]);el.min=x[1];el.max=x[2];el.step=1;});if($('sheetMaterialId').dataset.shippingDefaultApplied!=='1'){$('sheetMaterialId').value='osb_18';$('sheetMaterialId').dataset.shippingDefaultApplied='1';}const t=selectedSheetThickness(),ow=+$('widthMm').value+2*t,od=+$('depthMm').value+2*t,oh=+$('heightMm').value+2*t;$('shippingBoxOuterDimensions').textContent='Berekende buitenmaten: '+roundMm(ow)+' × '+roundMm(od)+' × '+roundMm(oh)+' mm bij '+roundMm(t)+' mm plaatdikte.';$('generateBtn').textContent='Genereer shipping box';}else{if($('sheetMaterialId'))delete $('sheetMaterialId').dataset.shippingDefaultApplied;['widthMm','depthMm','heightMm'].forEach(id=>{const el=$(id);el.removeAttribute('min');el.removeAttribute('max');el.removeAttribute('step');});}};
    const buildBaseRequest=request;
    request=function(){const value=buildBaseRequest(),isWorkbenchCabinet=value.product==='werkbankkast';value.enableWoodScrewCountersinks=isWorkbenchCabinet&&$('enableWoodScrewCountersinks').checked;value.enableOutsideEdgeChamfer=isWorkbenchCabinet&&$('enableOutsideEdgeChamfer').checked;value.machineBaseWorktopHeightMm=+$('machineBaseWorktopHeightMm').value;value.machineBaseWorktopMaterialId=$('machineBaseWorktopMaterialId').value;value.machineBaseLowerBeamProfileId=$('machineBaseLowerBeamProfileId').value;value.machineBaseWorktopBeamProfileId=$('machineBaseWorktopBeamProfileId').value;return value;};
    const buildMachineBaseRequest=request;
    request=function(){const value=buildMachineBaseRequest();value.machineBaseWorktopIntermediateBeamMaxSpacingMm=+$('machineBaseWorktopIntermediateBeamMaxSpacingMm').value;value.machineBaseFrontProtectionMode=$('machineBaseFrontProtectionMode').value;value.machineBaseControlCabinetWidthMm=+$('machineBaseControlCabinetWidthMm').value;value.machineBaseControlCabinetDepthMm=+$('machineBaseControlCabinetDepthMm').value;value.machineBaseControlCabinetHeightMm=+$('machineBaseControlCabinetHeightMm').value;value.machineBaseControlCabinetPosition=$('machineBaseControlCabinetPosition').value;value.machineBaseControlCabinetDoorCount=+$('machineBaseControlCabinetDoorCount').value;value.machineBaseControlCabinetHingeSide=$('machineBaseControlCabinetHingeSide').value;value.machineBaseFrontDoorCount=+$('machineBaseFrontDoorCount').value;value.machineBaseFrontSingleDoorHingeSide=$('machineBaseFrontSingleDoorHingeSide').value;value.robotCellIntermediateBeamMaxSpacingMm=+$('robotCellIntermediateBeamMaxSpacingMm').value;return value;};
    const buildShippingBoxRequest=request;
    request=function(){const value=buildShippingBoxRequest();value.shippingBoxIncludeHandles=$('shippingBoxIncludeHandles').checked;value.shippingBoxJointMode=$('shippingBoxJointMode').value;return value;};
    loadCatalog().then(()=>{syncProductUi();loadOrders();});
  </script>
</body>
</html>";
        }
    }
}
