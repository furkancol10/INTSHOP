<script>
  import {
    API,
    authHeader,
    durum,
    fiyatKolon,
    sepeteEkle,
  } from "../store.svelte.js";
  import { ozellikAlanlari, ozellikleriAyristir } from "../urunOzellikleri.js";
  import SepetWidget from "./SepetWidget.svelte";

  let { productId, geriDon } = $props();

  let urun = $state(null);
  let yukleniyor = $state(false);
  let hata = $state("");
  let ekleniyorId = $state(null);

  async function yukle() {
    if (!productId) return;
    yukleniyor = true;
    hata = "";
    urun = null;
    try {
      const res = await fetch(`${API}/api/shop/${productId}`, {
        headers: authHeader(),
      });
      if (!res.ok) throw new Error(await res.text());
      urun = await res.json();
    } catch (e) {
      hata = e instanceof Error ? e.message : String(e);
    } finally {
      yukleniyor = false;
    }
  }

  $effect(() => {
    productId;
    yukle();
  });

  let ozellikler = $derived.by(() => {
    if (!urun) return [];
    const degerler = ozellikleriAyristir(urun.attributes);
    return ozellikAlanlari(urun.category)
      .filter((a) => degerler[a.key])
      .map((a) => ({ label: a.label, deger: degerler[a.key] }));
  });

  async function ekle(dealerId) {
    ekleniyorId = dealerId;
    try {
      await sepeteEkle(productId, dealerId);
      durum.sepetPopup = true;
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    } finally {
      ekleniyorId = null;
    }
  }
</script>

<SepetWidget />

<button class="geri-btn" onclick={geriDon}>← Mağazaya Dön</button>

{#if yukleniyor}
  <div class="sentinel"><div class="spinner"></div></div>
{:else if hata}
  <p class="error">{hata}</p>
{:else if urun}
  <div class="urun-detay">
    <div class="detay-ust">
      {#if urun.image_url}
        <img
          src={urun.image_url}
          alt={urun.name}
          class="detay-resim"
          onerror={(e) => (e.currentTarget.src = "/images/placeholder.png")}
        />
      {/if}
      <div>
        <h2>{urun.name}</h2>
        {#if urun.category}<p class="detay-kategori">{urun.category}</p>{/if}
      </div>
    </div>

    {#if ozellikler.length}
      <h3>Ürün Özellikleri</h3>
      <dl class="ozellik-liste">
        {#each ozellikler as o}
          <div class="ozellik-satir">
            <dt>{o.label}</dt>
            <dd>{o.deger}</dd>
          </div>
        {/each}
      </dl>
    {/if}

    <h3>Satıcıyı seçin</h3>
    <div class="teklif-liste">
      {#each urun.teklifler as t}
        <div class="teklif-satir">
          <div class="teklif-bilgi">
            <span class="teklif-bayi">{t.dealer_name}</span>
            <span class="kart-stok">Stok: {t.stock}</span>
          </div>
          <span class="kart-fiyat">{fiyatKolon(t.price)}</span>
          <button
            class="sepet-btn"
            disabled={ekleniyorId === t.dealer_id}
            onclick={() => ekle(t.dealer_id)}
          >
            {ekleniyorId === t.dealer_id ? "Ekleniyor..." : "Sepete Ekle"}
          </button>
        </div>
      {/each}
    </div>
  </div>
{/if}
