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

<button class="bg-transparent border-none text-teal-600 text-[0.95rem] font-semibold cursor-pointer p-0 mb-4 hover:underline" onclick={geriDon}>← Mağazaya Dön</button>

{#if yukleniyor}
  <div class="h-[60px] flex items-center justify-center"><div class="spinner"></div></div>
{:else if hata}
  <p class="text-red-700 text-[0.85rem] m-0">{hata}</p>
{:else if urun}
  <div class="max-w-[700px]">
    <div class="flex gap-6 items-center mb-6">
      {#if urun.image_url}
        <img
          src={urun.image_url}
          alt={urun.name}
          class="w-40 h-40 object-contain rounded-xl border border-slate-100 shrink-0"
          onerror={(e) => (e.currentTarget.src = "/images/placeholder.png")}
        />
      {/if}
      <div>
        <h2 class="m-0 mb-[0.3rem]">{urun.name}</h2>
        {#if urun.category}<p class="m-0 text-slate-600 text-[0.9rem]">{urun.category}</p>{/if}
      </div>
    </div>

    {#if ozellikler.length}
      <h3>Ürün Özellikleri</h3>
      <dl class="m-0 mb-6 grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-x-6 gap-y-[0.6rem]">
        {#each ozellikler as o}
          <div class="flex justify-between gap-3 pb-2 border-b border-slate-100">
            <dt class="text-slate-600 text-[0.9rem]">{o.label}</dt>
            <dd class="m-0 font-semibold text-right">{o.deger}</dd>
          </div>
        {/each}
      </dl>
    {/if}

    <h3>Satıcıyı seçin</h3>
    <div class="flex flex-col gap-3">
      {#each urun.teklifler as t}
        <div class="flex items-center gap-6 border border-slate-100 rounded-xl py-[0.8rem] px-4 bg-white">
          <div class="flex flex-col gap-[0.2rem] flex-1">
            <span class="font-semibold">{t.dealer_name}</span>
            <span class="m-0 text-[0.8rem] text-slate-600">Stok: {t.stock}</span>
          </div>
          <span class="text-[1.3rem] font-bold text-blue-950 m-0">{fiyatKolon(t.price)}</span>
          <button
            class="mt-0 p-[0.4rem] bg-teal-600 text-white border-none rounded-lg text-[0.95rem] cursor-pointer transition-[background-color,transform] duration-200 hover:bg-teal-700"
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
