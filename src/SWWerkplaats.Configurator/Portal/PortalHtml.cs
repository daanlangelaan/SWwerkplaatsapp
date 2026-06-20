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
    button{border:0;border-radius:14px;padding:12px 15px;font-weight:760;cursor:pointer;background:var(--accent);color:#fff;box-shadow:0 7px 18px rgba(0,113,227,.20)}button.secondary{background:#2f3641;box-shadow:none}button.warn{background:var(--warn);box-shadow:0 7px 18px rgba(191,91,0,.18)}button.ghost{background:#eef0f4;color:#1d1d1f;box-shadow:none}button:disabled{opacity:.52;cursor:not-allowed}.toolbar{display:flex;gap:10px;flex-wrap:wrap;margin-top:16px}
    .generateBar{position:sticky;bottom:16px;margin-top:18px;padding:12px;border-radius:20px;background:rgba(255,255,255,.86);border:1px solid rgba(0,0,0,.08);box-shadow:0 14px 38px rgba(20,24,33,.12);backdrop-filter:blur(18px)}.generateBar button{width:100%;font-size:16px;padding:14px 16px}.dirtyNote{font-size:12px;color:var(--muted);text-align:center;margin-top:8px}
    .pricePanel{display:grid;grid-template-columns:1fr auto;gap:12px;align-items:start}.price{font-size:42px;font-weight:820;margin:4px 0 6px;line-height:1}.priceBreakdown{display:grid;grid-template-columns:repeat(6,minmax(105px,1fr));gap:8px;margin:10px 0 6px;max-width:1040px}.priceBreakdown span{display:block;border-radius:12px;background:#f5f6f8;padding:8px 10px;font-size:12px;color:var(--muted)}.priceBreakdown strong{display:block;color:var(--ink);font-size:15px;margin-top:2px}.muted{color:var(--muted)}.error{color:var(--danger);font-weight:760}.summaryLine{font-size:15px;color:var(--muted)}.lead{font-size:14px;color:var(--muted)}
    .previewGrid{display:grid;grid-template-columns:1.45fr .9fr;gap:18px}.sidePreviews{display:grid;gap:18px}.svgbox,.canvasbox,.orthobox{border:1px solid rgba(0,0,0,.08);border-radius:20px;background:linear-gradient(180deg,#fff,#fafbfc);overflow:hidden;min-height:260px}.svgbox svg{width:100%;height:auto;display:block}.canvasbox{position:relative;min-height:520px}.canvasbox canvas{position:absolute;inset:0;width:100%;height:100%;min-height:520px;display:block;cursor:grab}.orthobox{position:relative;min-height:330px}.orthobox canvas{width:100%;height:100%;min-height:330px;display:block}.canvasbox canvas:active{cursor:grabbing}.webglOn #assemblyFallbackCanvas{display:none}.webglOn #assemblyCanvas{display:block}#assemblyCanvas{display:none}.viewerHint{position:absolute;left:18px;bottom:18px;right:18px;display:flex;gap:10px;align-items:center;padding:10px 12px;border-radius:16px;background:rgba(255,255,255,.78);backdrop-filter:blur(14px);box-shadow:0 8px 24px rgba(20,24,33,.08);font-size:13px;color:var(--muted)}.viewerHint input{padding:0;background:transparent;box-shadow:none;border:0;accent-color:var(--accent)}.sectionHead{display:flex;align-items:center;justify-content:space-between;margin-bottom:10px}.badge{display:inline-flex;align-items:center;border-radius:999px;padding:6px 10px;background:#eef7f3;color:#1d7f5f;font-size:12px;font-weight:760}.viewActions{display:flex;align-items:center;gap:10px}.viewActions button{padding:8px 11px;border-radius:11px;font-size:12px;background:#eef0f4;color:#1d1d1f;box-shadow:none}.modal{display:none;position:fixed;inset:0;z-index:30;background:rgba(245,245,247,.72);backdrop-filter:blur(18px);padding:28px}.modalOn .modal{display:grid}.modalPanel{background:#fff;border-radius:26px;box-shadow:0 24px 80px rgba(20,24,33,.22);display:grid;grid-template-rows:auto 1fr;min-height:0;max-height:calc(100vh - 56px);overflow:hidden}.modalHead{display:flex;align-items:center;justify-content:space-between;padding:16px 18px;border-bottom:1px solid rgba(0,0,0,.08)}.modalBody{padding:18px;min-height:0;overflow:auto}.modalBody .canvasbox,.modalBody .orthobox{height:calc(100vh - 150px);min-height:520px}.modalBody .svgbox{max-height:none;overflow:visible}.modalBody .svgbox svg{max-width:none}.modalBody .nestingZoomHost{height:calc(100vh - 150px);overflow:auto;cursor:zoom-in}.modalBody .nestingZoomHost svg{max-width:none}.modalBody canvas{min-height:100%}
    table{width:100%;border-collapse:collapse;font-size:13px}th,td{text-align:left;border-bottom:1px solid var(--line);padding:10px 6px;vertical-align:top}th{color:var(--muted);font-weight:760}.pill{display:inline-block;border-radius:999px;padding:5px 9px;background:#eef0f4;color:#344054;font-weight:720;font-size:12px}.orderTools button{padding:8px 10px;border-radius:10px}
    .productOnlyWorkbench{display:none}.isWorkbench .productOnlyCabinet{display:none}.isWorkbench .productOnlyWorkbench{display:block}.isWorkbench .unitField{display:none}
    @media(max-width:1180px){main{grid-template-columns:1fr}.previewGrid{grid-template-columns:1fr}.choices{grid-template-columns:1fr}.hero h2{font-size:38px}}@media(max-width:680px){header{padding:0 18px}.topMeta{display:none}.start{padding:24px}.hero h2{font-size:34px}.row{grid-template-columns:1fr}.pricePanel{grid-template-columns:1fr}.price{font-size:34px}}
  </style>
</head>
<body>
  <header>
    <div class=""brand""><div class=""mark""></div><h1>SW Werkplaats Portal</h1></div>
    <div class=""headerTools""><div class=""topMeta"">Lokale MVP voor configuratie, prijs en freeswachtrij</div><button class=""stopPortal"" type=""button"" onclick=""stopPortal()"">Stop portal</button></div>
  </header>

  <section class=""start"" id=""start"">
    <div class=""hero"">
      <h2>Kies wat je wilt configureren.</h2>
      <p>Begin met een werktafel, cabinet of vakjeskast. Daarna zie je direct een prijsindicatie, nette visualisatie en de interne werkplaatsflow.</p>
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
      <button class=""choice"" type=""button"" onclick=""chooseProduct('cabinet')"">
        <div class=""choiceArt"">
          <img src=""/images/product-cabinet.png"" alt=""Voorbeeld cabinet"">
        </div>
        <div class=""choiceImageLabel"">Cabinet / kast</div>
        <h3>Cabinet / kast</h3>
        <p>Units, lades, deuren, legplanken, nesting en ordervrijgave voor productie.</p>
        <span>Configureer cabinet</span>
      </button>
      <button class=""choice"" type=""button"" onclick=""chooseProduct('vakjeskast')"">
        <div class=""choiceArt"">
          <img src=""/images/product-cabinet.png"" alt=""Voorbeeld vakjeskast"">
        </div>
        <div class=""choiceImageLabel"">Vakjeskast</div>
        <h3>Vakjeskast</h3>
        <p>Grid met kamdelen, achterwandsegmenten, positioneergroeven en nesting per plaatmateriaal.</p>
        <span>Configureer vakjeskast</span>
      </button>
    </div>
  </section>

  <main id=""app"">
    <div class=""stack"">
      <section class=""panel glass"" id=""configPanel"">
        <div class=""sectionHead""><h2>Klantconfigurator</h2><button class=""ghost"" type=""button"" onclick=""backToStart()"">Wijzig product</button></div>
        <label>Product</label>
        <select id=""product"" onchange=""syncProductUi()""><option value=""cabinet"">Cabinet / kast</option><option value=""werktafel"">Werktafel</option></select>
        <div class=""row"">
          <div><label>Breedte mm</label><input id=""widthMm"" type=""number"" value=""2400""></div>
          <div><label>Diepte mm</label><input id=""depthMm"" type=""number"" value=""600""></div>
        </div>
        <div class=""row"">
          <div><label>Hoogte mm</label><input id=""heightMm"" type=""number"" value=""900""></div>
          <div><label>Aantal stuks</label><input id=""quantity"" type=""number"" value=""1"" min=""1"" max=""99""></div>
        </div>
        <div class=""row"">
          <div class=""unitField""><label>Units</label><input id=""unitCount"" type=""number"" value=""4"" min=""1"" max=""12""></div>
        </div>
        <label>Plaatmateriaal</label><select id=""sheetMaterialId""></select>
        <div class=""row productOnlyCabinet"">
          <div><label>Plaatmateriaal lades</label><select id=""drawerMaterialId""></select></div>
          <div><label>Plaatmateriaal achterwand</label><select id=""backMaterialId""></select></div>
        </div>
        <div class=""productOnlyWorkbench"">
          <label>Profielmateriaal</label><select id=""profileMaterialId""></select>
          <div class=""checks"">
            <label><input id=""includeLowerShelf"" type=""checkbox""> Onderblad met uitsparingen</label>
            <label><input id=""includeMiddleShelf"" type=""checkbox""> Ligblad / tussenblad met uitsparingen</label>
          </div>
          <div class=""row"">
            <div><label>Hoogte onderblad mm</label><input id=""lowerShelfHeightMm"" type=""number"" value=""180""></div>
            <div><label>Hoogte ligblad mm</label><input id=""middleShelfHeightMm"" type=""number"" value=""450""></div>
          </div>
        </div>
        <div class=""row productOnlyCabinet"">
          <div><label>Legplanken per unit</label><input id=""defaultShelfCount"" type=""number"" value=""3"" min=""0""></div>
          <div><label>Legplanken/lades starten</label><select id=""shelfStartMode""><option value=""bottom"">Start onder</option><option value=""top"" selected>Start boven</option></select></div>
        </div>
        <div class=""row productOnlyCabinet"">
          <div><label>Lades per unit</label><input id=""defaultDrawerCount"" type=""number"" value=""1"" min=""0""></div>
        </div>
        <div class=""productOnlyCabinet"">
          <label>Deuren</label><select id=""doorMode""><option value=""geen"">Open vakken</option><option value=""links"">Draaideur links</option><option value=""rechts"">Draaideur rechts</option><option value=""sliding"">Schuifdeuren</option></select>
          <div class=""checks"">
            <label><input id=""includeBackPanel"" type=""checkbox"" checked> Achterwand toevoegen</label>
            <label><input id=""includeTopDrawer"" type=""checkbox""> Bovenlade per unit</label>
            <label><input id=""includeAdjustableShelfHoles"" type=""checkbox"" checked> Legplankgaten systeem 32</label>
          </div>
        </div>
        <div class=""generateBar"">
          <button id=""generateBtn"" type=""button"">Genereer kast</button>
          <div id=""dirtyNote"" class=""dirtyNote"">Pas instellingen aan en genereer opnieuw.</div>
        </div>
      </section>
      <section class=""panel glass"">
        <h2>Klant</h2>
        <label>Naam</label><input id=""customerName"" value=""Testklant"">
        <label>Email</label><input id=""customerEmail"" value=""klant@example.local"">
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
          <div class=""sectionHead""><h3>360 assembly</h3><div class=""viewActions""><span class=""muted"">sleep of draai</span><button type=""button"" onclick=""openViewer('assembly')"">Vergroot</button></div></div>
          <div class=""canvasbox"">
            <canvas id=""assemblyCanvas"" width=""980"" height=""620""></canvas>
            <canvas id=""assemblyFallbackCanvas"" width=""980"" height=""620""></canvas>
            <div class=""viewerHint""><span>360 graden</span><input id=""rotation"" type=""range"" min=""0"" max=""360"" value=""35""><span id=""partCount"">0 onderdelen</span></div>
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
      <div class=""modalHead""><h2 id=""modalTitle"">Visualisatie</h2><button class=""ghost"" type=""button"" onclick=""closeViewer()"">Sluiten</button></div>
      <div class=""modalBody"" id=""modalBody""></div>
    </div>
  </div>

  <script>
    let lastRequest=null,lastQuote=null,assemblyParts=[],rotationDeg=35,dragging=false,lastDragX=0,threePromise=null,threeApi=null,threeState=null,modalSource=null,nestingZoom=1,nestingBaseWidth=0,nestingBaseHeight=0,catalogData=null;
    const $=id=>document.getElementById(id);
    async function api(path,opts){const r=await fetch(path,opts);if(!r.ok)throw new Error(await r.text());return await r.json();}
    async function stopPortal(){const btn=document.querySelector('.stopPortal');if(btn){btn.disabled=true;btn.textContent='Stopt...';}try{const r=await api('/api/shutdown',{method:'POST'});document.body.innerHTML='<main style=""display:grid;place-items:center;min-height:100vh""><section class=""panel"" style=""max-width:520px;text-align:center""><h2>Portal gestopt</h2><p class=""muted"">'+r.message+'</p></section></main>';}catch(e){document.body.innerHTML='<main style=""display:grid;place-items:center;min-height:100vh""><section class=""panel"" style=""max-width:520px;text-align:center""><h2>Portal is gestopt</h2><p class=""muted"">Start de configurator opnieuw om verder te gaan.</p></section></main>';}}
    function productMeta(product){return catalogData&&catalogData.products?(catalogData.products.find(x=>x.Product===product)||null):null;}
    function chooseProduct(product){document.body.classList.add('appOn');$('product').value=product;$('quantity').value=1;const meta=productMeta(product);if(meta){$('widthMm').value=meta.DefaultWidthMm;$('depthMm').value=meta.DefaultDepthMm;$('heightMm').value=meta.DefaultHeightMm;$('unitCount').value=meta.DefaultUnitCount;$('defaultShelfCount').value=meta.DefaultShelfCount;$('defaultDrawerCount').value=meta.DefaultDrawerCount;$('shelfStartMode').value=meta.DefaultShelfStartMode||'bottom';}else if(product==='werktafel'){$('widthMm').value=1500;$('depthMm').value=750;$('heightMm').value=900;$('unitCount').value=1;$('defaultShelfCount').value=0;$('defaultDrawerCount').value=0;}else{$('widthMm').value=2400;$('depthMm').value=600;$('heightMm').value=900;$('unitCount').value=4;$('defaultShelfCount').value=3;$('shelfStartMode').value='top';$('defaultDrawerCount').value=1;}syncProductUi();quote();}
    function backToStart(){document.body.classList.remove('appOn');}
    function syncProductUi(){const product=$('product').value,isWorkbench=product==='werktafel',isCubby=product==='vakjeskast';document.body.classList.toggle('isWorkbench',isWorkbench);document.body.classList.toggle('isCubby',isCubby);$('generateBtn').textContent=isWorkbench?'Genereer tafel':(isCubby?'Genereer vakjeskast':'Genereer kast');markDirty();}
    function markDirty(){if($('orderBtn'))$('orderBtn').disabled=true;if($('mailQuoteBtn'))$('mailQuoteBtn').disabled=true;lastQuote=null;if($('dirtyNote'))$('dirtyNote').textContent='Instellingen gewijzigd. Genereer opnieuw voor actuele prijs en 3D assembly.';}
    function request(){return{product:$('product').value,widthMm:+$('widthMm').value,depthMm:+$('depthMm').value,heightMm:+$('heightMm').value,quantity:Math.max(1,+$('quantity').value||1),unitCount:+$('unitCount').value,sheetMaterialId:$('sheetMaterialId').value,drawerMaterialId:$('drawerMaterialId').value,backMaterialId:$('backMaterialId').value,profileMaterialId:$('profileMaterialId').value,includeBackPanel:$('includeBackPanel').checked,includeTopDrawer:$('includeTopDrawer').checked,includeAdjustableShelfHoles:$('includeAdjustableShelfHoles').checked,defaultShelfCount:+$('defaultShelfCount').value,shelfStartMode:$('shelfStartMode').value,defaultDrawerCount:+$('defaultDrawerCount').value,doorMode:$('doorMode').value,customerName:$('customerName').value,customerEmail:$('customerEmail').value,customerPhone:$('customerPhone').value,notes:$('notes').value,includeLowerShelf:$('includeLowerShelf').checked,includeMiddleShelf:$('includeMiddleShelf').checked,lowerShelfHeightMm:+$('lowerShelfHeightMm').value,middleShelfHeightMm:+$('middleShelfHeightMm').value};}
    async function loadCatalog(){const c=await api('/api/catalog');catalogData=c;if(c.products&&c.products.length){$('product').innerHTML=c.products.map(x=>`<option value=""${x.Product}"">${x.Name}</option>`).join('');}const sheetOptions=c.sheets.map(x=>`<option value=""${x.Id}"">${x.Name}</option>`).join('');$('sheetMaterialId').innerHTML=sheetOptions;$('drawerMaterialId').innerHTML=sheetOptions;$('backMaterialId').innerHTML=sheetOptions;$('profileMaterialId').innerHTML=c.profiles.map(x=>`<option value=""${x.Id}"">${x.Name}</option>`).join('');$('sheetMaterialId').value='betonplex_18';$('drawerMaterialId').value='multiplex_15';$('backMaterialId').value='multiplex_15';$('profileMaterialId').value='alu_profile_40x40';}
    async function quote(){try{$('message').textContent='Genereren...';$('generateBtn').disabled=true;lastRequest=request();lastQuote=await api('/api/quote',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(lastRequest)});$('summary').textContent=lastQuote.Summary;$('price').textContent='EUR '+Number(lastQuote.PriceIncVat).toFixed(2)+' incl. btw';renderPriceBreakdown(lastQuote);$('lead').textContent=lastQuote.LeadTime+' - excl. btw EUR '+Number(lastQuote.PriceExVat).toFixed(2);$('nestingPreview').innerHTML=lastQuote.NestingSvg;resetNestingZoom();assemblyParts=lastQuote.Assembly3D||[];$('partCount').textContent=assemblyParts.length+' onderdelen';renderAssembly();renderOrthoPreview();loadThreeViewer().then(()=>{renderAssembly();renderOrthoPreview();});$('orderBtn').disabled=false;$('mailQuoteBtn').disabled=false;$('dirtyNote').textContent='Actuele configuratie gegenereerd.';$('message').textContent='Prijs klaar. Controleer preview en zet bij akkoord om naar order.';}catch(e){$('message').innerHTML='<span class=""error"">'+e.message+'</span>';}finally{$('generateBtn').disabled=false;}}
    function renderPriceBreakdown(q){const money=v=>'EUR '+Number(v||0).toFixed(2);$('priceBreakdown').innerHTML=[['Plaatmateriaal ex',q.Material],['Beslag ex',q.Hardware],['Machine ex',q.Machine],['Arbeid ex',q.Labour],['Opslag/marge',q.Margin],['Btw',q.Vat]].map(x=>`<span>${x[0]}<strong>${money(x[1])}</strong></span>`).join('');}
    function mailQuote(){if(!lastQuote||!lastRequest){$('message').textContent='Genereer eerst een actuele offerte.';return;}const subject='Offerte '+lastQuote.ProductName+' - '+lastQuote.QuoteId;const body=['Beste '+(lastRequest.CustomerName||'klant')+',','','Hierbij de richtofferte voor je configuratie.','',lastQuote.Summary,'Prijs excl. btw: EUR '+Number(lastQuote.PriceExVat).toFixed(2),'Btw: EUR '+Number(lastQuote.Vat).toFixed(2),'Prijs incl. btw: EUR '+Number(lastQuote.PriceIncVat).toFixed(2),lastQuote.LeadTime,'','Let op: dit is een MVP-richtprijs. Na technische controle bevestigen wij de definitieve productiegegevens.','','Met vriendelijke groet,','SW Werkplaats'].join('\\n');window.location.href='mailto:'+(lastRequest.CustomerEmail||'')+'?subject='+encodeURIComponent(subject)+'&body='+encodeURIComponent(body);}
    async function order(){try{if(!lastRequest)await quote();$('message').textContent='Order maken...';const o=await api('/api/orders',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(lastRequest)});$('message').textContent=o.Message+' '+o.Order.OrderId;await loadOrders();}catch(e){$('message').innerHTML='<span class=""error"">'+e.message+'</span>';}}
    async function release(id){await api('/api/orders/'+encodeURIComponent(id)+'/release',{method:'POST'});await loadOrders();}
    async function loadOrders(){const list=await api('/api/orders');$('orders').innerHTML=list.map(o=>`<tr><td>${o.OrderId}<br><span class=""muted"">${o.ProductName}</span></td><td><span class=""pill"">${o.Status}</span></td><td>${o.CustomerName||''}</td><td class=""orderTools""><button class=""secondary"" onclick=""release('${o.OrderId}')"">Vrijgeven</button></td></tr>`).join('')||'<tr><td colspan=""4"" class=""muted"">Nog geen orders.</td></tr>';}
    $('quoteBtn').onclick=quote;$('generateBtn').onclick=quote;$('mailQuoteBtn').onclick=mailQuote;$('orderBtn').onclick=order;$('refreshOrders').onclick=loadOrders;$('product').onchange=syncProductUi;
    document.querySelectorAll('#configPanel input,#configPanel select').forEach(el=>{el.addEventListener('input',markDirty);el.addEventListener('change',()=>{if(el.id==='product')syncProductUi();else markDirty();});});
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
    function openViewer(kind){const body=$('modalBody');body.innerHTML='';modalSource=kind;if(kind==='assembly'){body.appendChild(document.querySelector('.canvasbox'));$('modalTitle').textContent='360 assembly';}else if(kind==='ortho'){body.appendChild($('productPreview'));$('modalTitle').textContent='Voor- en zijaanzicht';}else{body.appendChild($('nestingPreview'));$('nestingPreview').classList.add('nestingZoomHost');$('modalTitle').textContent='Nesting';applyNestingZoom();}document.body.classList.add('modalOn');setTimeout(()=>{renderAssembly();renderOrthoPreview();if(kind==='nesting')applyNestingZoom();},40);}
    function closeViewer(){const kind=modalSource;if(kind==='assembly'){document.querySelector('.previewGrid > section').appendChild(document.querySelector('.canvasbox'));}else if(kind==='ortho'){document.querySelector('.sidePreviews section:first-child').appendChild($('productPreview'));}else if(kind==='nesting'){$('nestingPreview').classList.remove('nestingZoomHost');resetNestingZoom();document.querySelector('.sidePreviews section:nth-child(2)').appendChild($('nestingPreview'));}document.body.classList.remove('modalOn');modalSource=null;setTimeout(()=>{renderAssembly();renderOrthoPreview();},40);}
    function resetNestingZoom(){nestingZoom=1;nestingBaseWidth=0;nestingBaseHeight=0;applyNestingZoom();}
    function applyNestingZoom(){const box=$('nestingPreview'),svg=box?box.querySelector('svg'):null;if(!svg)return;if(!nestingBaseWidth){nestingBaseWidth=parseFloat(svg.getAttribute('width'))||svg.viewBox.baseVal.width||svg.getBoundingClientRect().width;nestingBaseHeight=parseFloat(svg.getAttribute('height'))||svg.viewBox.baseVal.height||svg.getBoundingClientRect().height;}if(box.classList.contains('nestingZoomHost')){svg.style.width=(nestingBaseWidth*nestingZoom)+'px';svg.style.height=(nestingBaseHeight*nestingZoom)+'px';}else{svg.style.width='';svg.style.height='';}}
    $('nestingPreview').addEventListener('wheel',e=>{if(modalSource!=='nesting'||!e.ctrlKey)return;e.preventDefault();const box=$('nestingPreview'),rect=box.getBoundingClientRect(),oldZoom=nestingZoom,mouseX=e.clientX-rect.left+box.scrollLeft,mouseY=e.clientY-rect.top+box.scrollTop;nestingZoom=Math.max(.25,Math.min(6,nestingZoom*(e.deltaY<0?1.12:.89)));applyNestingZoom();const ratio=nestingZoom/oldZoom;box.scrollLeft=mouseX*ratio-(e.clientX-rect.left);box.scrollTop=mouseY*ratio-(e.clientY-rect.top);},{passive:false});
    function renderAssembly(){
      if(threeApi){renderThreeAssembly();return;}
      const c=$('assemblyFallbackCanvas'),ctx=c.getContext('2d'),box=c.getBoundingClientRect(),dpr=window.devicePixelRatio||1;
      const w=Math.max(520,Math.floor(box.width*dpr)),h=Math.max(420,Math.floor(box.height*dpr));if(c.width!==w||c.height!==h){c.width=w;c.height=h;}
      ctx.clearRect(0,0,w,h);const g=ctx.createLinearGradient(0,0,0,h);g.addColorStop(0,'#ffffff');g.addColorStop(1,'#eef2f7');ctx.fillStyle=g;ctx.fillRect(0,0,w,h);
      if(!assemblyParts.length){ctx.fillStyle='#6e6e73';ctx.font=`${16*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText('Bereken eerst een configuratie voor de 360 assembly.',34*dpr,54*dpr);return;}
      let minX=1e9,maxX=-1e9,minY=1e9,maxY=-1e9,minZ=1e9,maxZ=-1e9;
      assemblyParts.forEach(p=>{minX=Math.min(minX,p.Xmm-p.SizeXmm/2);maxX=Math.max(maxX,p.Xmm+p.SizeXmm/2);minY=Math.min(minY,p.Ymm-p.SizeYmm/2);maxY=Math.max(maxY,p.Ymm+p.SizeYmm/2);minZ=Math.min(minZ,p.Zmm-p.SizeZmm/2);maxZ=Math.max(maxZ,p.Zmm+p.SizeZmm/2);});
      const cx=(minX+maxX)/2,cz=(minZ+maxZ)/2,cy=minY;const span=Math.max(maxX-minX,maxZ-minZ,(maxY-minY)*1.25,1);const scale=Math.min(w*.72/span,h*.70/span);const ang=rotationDeg*Math.PI/180,pitch=-.34;
      function rot(pt){let x=pt.x-cx,y=pt.y-cy,z=pt.z-cz;let rx=x*Math.cos(ang)-z*Math.sin(ang),rz=x*Math.sin(ang)+z*Math.cos(ang);let ry=y*Math.cos(pitch)-rz*Math.sin(pitch);rz=y*Math.sin(pitch)+rz*Math.cos(pitch);return{x:rx,y:ry,z:rz};}
      function proj(pt){const r=rot(pt),persp=900/(900+r.z*.22);return{x:w/2+r.x*scale*persp,y:h*.70-r.y*scale*persp,z:r.z};}
      const faces=[];assemblyParts.forEach((p,i)=>addBoxFaces(faces,p,i,proj));
      faces.sort((a,b)=>a.depth-b.depth);
      ctx.save();ctx.shadowColor='rgba(15,23,42,.13)';ctx.shadowBlur=10*dpr;ctx.shadowOffsetY=5*dpr;
      faces.forEach(f=>{ctx.beginPath();f.points.forEach((pt,i)=>i?ctx.lineTo(pt.x,pt.y):ctx.moveTo(pt.x,pt.y));ctx.closePath();ctx.fillStyle=f.fill;ctx.fill();ctx.shadowColor='transparent';ctx.strokeStyle='rgba(71,84,103,.45)';ctx.lineWidth=1*dpr;ctx.stroke();ctx.shadowColor='rgba(15,23,42,.10)';});
      ctx.restore();drawGround(ctx,w,h,dpr);ctx.fillStyle='#1d1d1f';ctx.font=`${18*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText('Volledige assembly 3D',24*dpr,34*dpr);ctx.fillStyle='#6e6e73';ctx.font=`${13*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText('Sleep horizontaal of gebruik de slider voor 360 graden rotatie.',24*dpr,56*dpr);
    }
    function addBoxFaces(faces,p,index,proj){
      const x=p.Xmm,y=p.Ymm,z=p.Zmm,sx=p.SizeXmm/2,sy=p.SizeYmm/2,sz=p.SizeZmm/2;
      const v=[{x:x-sx,y:y-sy,z:z-sz},{x:x+sx,y:y-sy,z:z-sz},{x:x+sx,y:y+sy,z:z-sz},{x:x-sx,y:y+sy,z:z-sz},{x:x-sx,y:y-sy,z:z+sz},{x:x+sx,y:y-sy,z:z+sz},{x:x+sx,y:y+sy,z:z+sz},{x:x-sx,y:y+sy,z:z+sz}].map(proj);
      const sheet=p.Kind==='sheet',base=sheet?['#f3eadc','#e3d4bd','#fffaf0']:['#d9e0ea','#c4cfdc','#eef2f7'];
      [[0,1,2,3,base[1]],[4,5,6,7,base[0]],[3,2,6,7,base[2]],[0,4,7,3,base[0]],[1,5,6,2,base[1]],[0,1,5,4,base[1]]].forEach(f=>faces.push({points:[v[f[0]],v[f[1]],v[f[2]],v[f[3]]],depth:(v[f[0]].z+v[f[1]].z+v[f[2]].z+v[f[3]].z)/4+index*.001,fill:f[4]}));
    }
    function drawGround(ctx,w,h,dpr){ctx.save();ctx.globalAlpha=.55;ctx.fillStyle='#dfe4eb';ctx.beginPath();ctx.ellipse(w/2,h*.78,w*.32,h*.045,0,0,Math.PI*2);ctx.fill();ctx.restore();}
    function renderOrthoPreview(){
      const c=$('orthoCanvas');if(!c||!assemblyParts.length)return;const ctx=c.getContext('2d'),box=c.getBoundingClientRect(),dpr=window.devicePixelRatio||1,w=Math.max(520,Math.floor(box.width*dpr)),h=Math.max(320,Math.floor(box.height*dpr));if(c.width!==w||c.height!==h){c.width=w;c.height=h;}ctx.clearRect(0,0,w,h);const g=ctx.createLinearGradient(0,0,0,h);g.addColorStop(0,'#ffffff');g.addColorStop(1,'#f1f4f8');ctx.fillStyle=g;ctx.fillRect(0,0,w,h);
      const bounds=assemblyBounds(),pad=34*dpr,gap=24*dpr,frontW=(w-pad*2-gap)*.66,sideW=(w-pad*2-gap)-frontW,drawH=h-pad*2-36*dpr;
      drawOrthoSet(ctx,pad,pad+34*dpr,frontW,drawH,bounds,'front',dpr);drawOrthoSet(ctx,pad+frontW+gap,pad+34*dpr,sideW,drawH,bounds,'side',dpr);
      ctx.fillStyle='#1d1d1f';ctx.font=`${15*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText(lastQuote?lastQuote.ProductName+' assembly':'Assembly',pad,24*dpr);ctx.fillStyle='#6e6e73';ctx.font=`${11*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText('Zelfde assembly-data als 360 view',pad,40*dpr);
    }
    function drawOrthoSet(ctx,x,y,w,h,b,mode,dpr){
      const useX=mode==='front',spanX=useX?(b.maxX-b.minX):(b.maxZ-b.minZ),spanY=b.maxY-b.minY,scale=Math.min(w/Math.max(spanX,1),h/Math.max(spanY,1))*.92,ox=x+w/2,oy=y+h*.95;
      const viewCenter=useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2,drawLeft=ox+(0-spanX/2)*scale,drawRight=ox+(spanX/2)*scale,drawTop=oy-(spanY)*scale,drawBottom=oy;
      const list=[...assemblyParts].sort((a,bp)=>(mode==='front'?a.Zmm-bp.Zmm:a.Xmm-bp.Xmm));list.forEach(p=>{const px=useX?p.Xmm:p.Zmm,psx=useX?p.SizeXmm:p.SizeZmm,py=p.Ymm,psy=p.SizeYmm;const rx=ox+(px-(useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2))*scale-psx*scale/2,ry=oy-(py-b.minY)*scale-psy*scale/2,rw=Math.max(1,psx*scale),rh=Math.max(1,psy*scale);ctx.fillStyle=p.Kind==='sheet'?'rgba(234,220,199,.78)':'rgba(202,212,224,.78)';ctx.strokeStyle='rgba(102,112,133,.55)';ctx.lineWidth=1*dpr;ctx.fillRect(rx,ry,rw,rh);ctx.strokeRect(rx,ry,rw,rh);(p.Pockets||[]).forEach(g=>{const gx=useX?g.Xmm:g.Zmm,gy=g.Ymm,gsx=useX?g.SizeXmm:g.SizeZmm,gsy=g.SizeYmm;if(gx+gsx/2<px-psx/2||gx-gsx/2>px+psx/2||gy+gsy/2<py-psy/2||gy-gsy/2>py+psy/2)return;ctx.fillStyle='rgba(80,72,60,.16)';ctx.strokeStyle='rgba(80,72,60,.52)';ctx.setLineDash([5*dpr,4*dpr]);ctx.fillRect(ox+(gx-(useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2))*scale-gsx*scale/2,oy-(gy-b.minY)*scale-gsy*scale/2,Math.max(1,gsx*scale),Math.max(1,gsy*scale));ctx.strokeRect(ox+(gx-(useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2))*scale-gsx*scale/2,oy-(gy-b.minY)*scale-gsy*scale/2,Math.max(1,gsx*scale),Math.max(1,gsy*scale));ctx.setLineDash([]);});(p.Holes||[]).forEach(hole=>{if(mode==='side'&&hole.DepthMm>0&&/^Zijwand/i.test(p.Name||''))return;const hx=useX?hole.Xmm:hole.Zmm,hy=hole.Ymm;if(hx<px-psx/2||hx>px+psx/2||hy<py-psy/2||hy>py+psy/2)return;ctx.beginPath();ctx.arc(ox+(hx-(useX?(b.minX+b.maxX)/2:(b.minZ+b.maxZ)/2))*scale,oy-(hy-b.minY)*scale,Math.max(1.5*dpr,hole.DiameterMm*scale*.45),0,Math.PI*2);ctx.fillStyle='rgba(52,64,84,.72)';ctx.fill();});});
      ctx.strokeStyle='#98a2b3';ctx.lineWidth=1*dpr;ctx.strokeRect(x,y,w,h);ctx.fillStyle='#667085';ctx.font=`${11*dpr}px -apple-system,Segoe UI,Arial`;ctx.fillText(mode==='front'?'Vooraanzicht':'Zijaanzicht',x+8*dpr,y+17*dpr);
      drawDimension(ctx,drawLeft,drawBottom+18*dpr,drawRight,drawBottom+18*dpr,Math.round(spanX)+' mm '+(useX?'breed':'diep'),dpr,false);
      drawDimension(ctx,drawLeft-18*dpr,drawBottom,drawLeft-18*dpr,drawTop,Math.round(spanY)+' mm hoog',dpr,true);
    }
    function drawDimension(ctx,x1,y1,x2,y2,label,dpr,vertical){
      ctx.save();ctx.strokeStyle='#667085';ctx.fillStyle='#475467';ctx.lineWidth=1*dpr;ctx.beginPath();ctx.moveTo(x1,y1);ctx.lineTo(x2,y2);ctx.stroke();drawTick(ctx,x1,y1,vertical,dpr);drawTick(ctx,x2,y2,vertical,dpr);ctx.font=`${10*dpr}px -apple-system,Segoe UI,Arial`;if(vertical){ctx.translate(x1-8*dpr,(y1+y2)/2);ctx.rotate(-Math.PI/2);ctx.textAlign='center';ctx.fillText(label,0,0);}else{ctx.textAlign='center';ctx.fillText(label,(x1+x2)/2,y1+14*dpr);}ctx.restore();
    }
    function drawTick(ctx,x,y,vertical,dpr){ctx.beginPath();if(vertical){ctx.moveTo(x-4*dpr,y);ctx.lineTo(x+4*dpr,y);}else{ctx.moveTo(x,y-4*dpr);ctx.lineTo(x,y+4*dpr);}ctx.stroke();}
    function assemblyBounds(){let minX=1e9,maxX=-1e9,minY=1e9,maxY=-1e9,minZ=1e9,maxZ=-1e9;assemblyParts.forEach(p=>{minX=Math.min(minX,p.Xmm-p.SizeXmm/2);maxX=Math.max(maxX,p.Xmm+p.SizeXmm/2);minY=Math.min(minY,p.Ymm-p.SizeYmm/2);maxY=Math.max(maxY,p.Ymm+p.SizeYmm/2);minZ=Math.min(minZ,p.Zmm-p.SizeZmm/2);maxZ=Math.max(maxZ,p.Zmm+p.SizeZmm/2);});return{minX,maxX,minY,maxY,minZ,maxZ};}
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
      if(!threeState){const renderer=new THREE.WebGLRenderer({canvas:c,antialias:true,alpha:true});renderer.setPixelRatio(Math.min(window.devicePixelRatio||1,2));renderer.setClearColor(0xf8fafc,1);const scene=new THREE.Scene();const camera=new THREE.OrthographicCamera(-1,1,1,-1,.1,10000);const controls=new OrbitControls(camera,c);controls.enableDamping=true;controls.dampingFactor=.08;controls.enablePan=false;controls.enableZoom=true;controls.zoomSpeed=1.15;controls.minZoom=.06;controls.maxZoom=9;scene.add(new THREE.HemisphereLight(0xffffff,0x9aa6b2,2.2));const key=new THREE.DirectionalLight(0xffffff,2.8);key.position.set(3,5,4);scene.add(key);threeState={renderer,scene,camera,controls,group:new THREE.Group(),lastKey:''};scene.add(threeState.group);controls.addEventListener('change',()=>threeState.renderer.render(threeState.scene,threeState.camera));c.addEventListener('wheel',e=>e.preventDefault(),{passive:false});}
      const key=JSON.stringify(assemblyParts.map(p=>[p.Name,p.Kind,p.Xmm,p.Ymm,p.Zmm,p.SizeXmm,p.SizeYmm,p.SizeZmm,(p.Pockets||[]).map(g=>[g.Xmm,g.Ymm,g.Zmm,g.SizeXmm,g.SizeYmm,g.SizeZmm,g.Plane]),(p.Holes||[]).map(h=>[h.Xmm,h.Ymm,h.Zmm,h.DiameterMm,h.DepthMm,h.Plane])]));
      threeState.renderer.setSize(w,h,false);threeState.camera.left=-w/2;threeState.camera.right=w/2;threeState.camera.top=h/2;threeState.camera.bottom=-h/2;threeState.camera.updateProjectionMatrix();
      if(threeState.lastKey!==key){threeState.group.clear();buildThreeParts(THREE,threeState.group);fitThreeCamera(THREE,threeState.camera,threeState.controls);threeState.lastKey=key;}
      threeState.group.rotation.y=rotationDeg*Math.PI/180;threeState.controls.update();threeState.renderer.render(threeState.scene,threeState.camera);
    }
    function buildThreeParts(THREE,group){
      assemblyParts.forEach(p=>{
        const material=new THREE.MeshStandardMaterial({color:p.Kind==='pocket'?0x6f6658:(p.Kind==='sheet'?0xeadcc7:0xcad4e0),roughness:.62,metalness:p.Kind==='profile'?0.18:0,transparent:p.Kind==='pocket'||p.Kind==='profile',opacity:p.Kind==='pocket'?0.62:(p.Kind==='profile'?0.9:1),side:THREE.DoubleSide});
        const realPocketSheet=p.Kind==='sheet'&&(p.Pockets||[]).some(g=>g.Plane==='y'&&g.SizeYmm>1.5&&g.SizeXmm<p.SizeXmm*.2);
        const realPocketZ=p.Kind==='sheet'&&!realPocketSheet&&(p.Pockets||[]).some(g=>g.Plane==='z'&&g.SizeZmm>1.5);
        const realPocketX=p.Kind==='sheet'&&!realPocketSheet&&!realPocketZ&&(p.Pockets||[]).some(g=>g.Plane==='x'&&g.SizeXmm>1.5);
        if(realPocketZ){addPocketedVerticalXPart(THREE,group,p,material);return;}
        if(realPocketX){addPocketedVerticalZPart(THREE,group,p,material);return;}
        const geo=realPocketSheet?buildPocketedSheetGeometry(THREE,p):new THREE.BoxGeometry(p.SizeXmm,p.SizeYmm,p.SizeZmm);
        const mesh=new THREE.Mesh(geo,material);mesh.position.set(p.Xmm,p.Ymm,p.Zmm);group.add(mesh);
        const edges=new THREE.LineSegments(new THREE.EdgesGeometry(geo),new THREE.LineBasicMaterial({color:p.Kind==='pocket'?0x3f3a33:0x667085,transparent:true,opacity:p.Kind==='pocket'?.62:.48}));edges.position.copy(mesh.position);group.add(edges);
        addThreeHoles(THREE,group,p);
      });
      const floorGeo=new THREE.CircleGeometry(900,64),floorMat=new THREE.MeshBasicMaterial({color:0xdfe4eb,transparent:true,opacity:.42});const floor=new THREE.Mesh(floorGeo,floorMat);floor.rotation.x=-Math.PI/2;floor.scale.set(1.55,.55,1);floor.position.y=-8;group.add(floor);
    }
    function addThreeHoles(THREE,group,p){
      (p.Holes||[]).forEach(h=>{const hg=new THREE.CircleGeometry(Math.max(3,h.DiameterMm/2),18),hm=new THREE.MeshBasicMaterial({color:0x344054,transparent:true,opacity:.72,side:THREE.DoubleSide}),hole=new THREE.Mesh(hg,hm);hole.position.set(h.Xmm,h.Ymm,h.Zmm);if(h.Plane==='x')hole.rotation.y=Math.PI/2;else if(h.Plane==='y')hole.rotation.x=-Math.PI/2;group.add(hole);});
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
      const pts=[new THREE.Vector2(x0,y0)];
      merged.forEach(r=>{pts.push(new THREE.Vector2(r.a,y0),new THREE.Vector2(r.a,y0+r.d),new THREE.Vector2(r.b,y0+r.d),new THREE.Vector2(r.b,y0));});
      pts.push(new THREE.Vector2(x1,y0),new THREE.Vector2(x1,y1),new THREE.Vector2(x0,y1));
      const shape=new THREE.Shape(pts),geo=new THREE.ExtrudeGeometry(shape,{depth:p.SizeZmm,bevelEnabled:false});
      geo.translate(0,0,-p.SizeZmm/2);geo.computeVertexNormals();return geo;
    }
    function fitThreeCamera(THREE,camera,controls){
      const box=new THREE.Box3().setFromObject(threeState.group),size=box.getSize(new THREE.Vector3()),center=box.getCenter(new THREE.Vector3()),span=Math.max(size.x,size.y,size.z,1);camera.position.set(center.x+span*.9,center.y+span*.62,center.z+span*.95);camera.lookAt(center);camera.zoom=Math.min(1.8,Math.max(.06,Math.min(threeState.renderer.domElement.width*.54/Math.max(size.x,size.z,1),threeState.renderer.domElement.height*.58/Math.max(size.y,1))));camera.updateProjectionMatrix();controls.target.copy(center);controls.update();
    }
    loadCatalog().then(()=>{syncProductUi();loadOrders();});
  </script>
</body>
</html>";
        }
    }
}
