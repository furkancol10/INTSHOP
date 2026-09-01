<script>
  import { onMount } from "svelte";
  import {
    API,
    authHeader,
    durum,
    veri,
    fiyatKolon,
  } from "../store.svelte.js";
  import SepetWidget from "./SepetWidget.svelte";

  function detayaGit(productId) {
    durum.secilenUrunId = productId;
    durum.aktifSekme = "urun-detay";
  }

  let secilenKategori = $state("");
  let arama = $state("");
  let aramaZaman;
  let ilkYukleme = true;

  let shopData = $state([]);
  let shopOffset = $state(0);
  let hepsiYuklendi = $state(false);
  let yukleniyor = $state(false);
  let sentinel = $state(null);
  const limit = 14;

  async function loadShop() {
    if (yukleniyor || hepsiYuklendi) return;
    yukleniyor = true;
    try {
      const p = new URLSearchParams({ limit, offset: shopOffset });
      if (secilenKategori) p.set("kategori", secilenKategori);
      if (arama) p.set("arama", arama);

      const res = await fetch(`${API}/api/shop?${p}`, {
        headers: authHeader(),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const yeni = await res.json();
      if (yeni.length < limit) hepsiYuklendi = true;
      shopData = [...shopData, ...yeni];
      shopOffset += yeni.length;
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    } finally {
      yukleniyor = false;
    }
  }

  $effect(() => {
    secilenKategori;
    arama;
    clearTimeout(aramaZaman);
    aramaZaman = setTimeout(() => {
      shopData = [];
      shopOffset = 0;
      hepsiYuklendi = false;
      loadShop();
    }, 300);
  });

  $effect(() => {
    if (!sentinel) return;
    const gozlemci = new IntersectionObserver(
      (girisler) => {
        if (girisler[0].isIntersecting) loadShop();
      },
      { rootMargin: "200px" },
    );
    gozlemci.observe(sentinel);
    return () => gozlemci.disconnect();
  });

  onMount(async () => {
    const res = await fetch(`${API}/api/categories`, { headers: authHeader() });
    if (res.ok) veri.categories = await res.json();
    await loadShop();
  });
</script>

<SepetWidget />

<div class="flex items-end justify-between mb-5 flex-wrap gap-3">
  <div>
    <h2 class="text-3xl font-bold tracking-tight m-0 bg-gradient-to-r from-teal-700 to-purple-700 bg-clip-text text-transparent">Mağaza</h2>
    {#if shopData.length}
      <p class="m-0 mt-1 text-sm text-slate-500">{shopData.length} ürün listeleniyor</p>
    {/if}
  </div>
</div>

<div class="flex gap-3 items-center flex-wrap mb-6">
  <div class="relative flex-1 max-w-[340px] min-w-[220px]">
    <span class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none">
      <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M15.5 14h-.79l-.28-.27A6.47 6.47 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/></svg>
    </span>
    <input
      bind:value={arama}
      placeholder="Ürün ara..."
      class="w-full py-[0.6rem] pl-10 pr-4 border border-slate-200 rounded-full text-[0.95rem] bg-white shadow-sm transition-all duration-200 focus:outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/15 hover:border-slate-300"
    />
  </div>
  <select
    bind:value={secilenKategori}
    class="py-[0.6rem] px-4 border border-slate-200 rounded-full text-[0.95rem] bg-white shadow-sm cursor-pointer transition-all duration-200 focus:outline-none focus:border-teal-500 focus:ring-4 focus:ring-teal-500/15 hover:border-slate-300"
  >
    <option value="">Tüm kategoriler</option>
    {#each veri.categories.filter((k) => !k.parent_id) as ust}
    <optgroup label={ust.name}>
      <option value={ust.id}>{ust.name} (tümü)</option>
      {#each veri.categories.filter((k) => k.parent_id === ust.id) as alt}
        <option value={alt.id}>{alt.name}</option>
      {/each}
    </optgroup>
  {/each}
  </select>
</div>

{#if shopData.length}
  <div class="grid grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-6">
    {#each shopData as urun, i}
      <button
        class="group relative overflow-hidden border border-slate-200/80 rounded-2xl p-4 flex flex-col gap-3 bg-white [font:inherit] text-left cursor-pointer shadow-sm transition-all duration-300 hover:border-teal-500/60 hover:shadow-xl hover:shadow-teal-900/10 hover:-translate-y-1 active:scale-[0.98] animate-fade-in-up"
        style="animation-delay: {(i % limit) * 45}ms"
        onclick={() => detayaGit(urun.product_id)}
      >
        <div class="relative w-full h-40 rounded-xl overflow-hidden bg-slate-50 flex items-center justify-center">
          {#if urun.image_url}
            <img
              src={urun.image_url}
              alt={urun.name}
              class="w-full h-full object-contain transition-transform duration-500 group-hover:scale-110"
              onerror={(e) => (e.currentTarget.src = "/images/placeholder.png")}
            />
          {:else}
            <svg viewBox="0 0 24 24" width="40" height="40" fill="currentColor" class="text-slate-300"><path d="M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z"/></svg>
          {/if}
          {#if urun.bayi_sayisi > 1}
            <span class="absolute top-2 right-2 bg-teal-600/90 text-white text-[0.7rem] font-semibold px-2 py-0.5 rounded-full backdrop-blur-sm">
              {urun.bayi_sayisi} bayi
            </span>
          {/if}
        </div>

        <h3 class="m-0 text-[0.98rem] font-semibold leading-snug line-clamp-2 group-hover:text-teal-700 transition-colors">{urun.name}</h3>

        <p class="m-0 text-[0.82rem] text-slate-500">
          En uygun: <strong class="font-semibold text-orange-600">{urun.dealer_name}</strong>
        </p>

        <p class="text-[1.35rem] font-bold text-blue-950 m-0 mt-auto">{fiyatKolon(urun.price)}</p>
      </button>
    {/each}
  </div>
  {#if !hepsiYuklendi}
    <div bind:this={sentinel} class="h-[60px] flex items-center justify-center">
      {#if yukleniyor}<div class="spinner"></div>{/if}
    </div>
  {/if}
{:else if yukleniyor}
  <div class="grid grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-6">
    {#each Array(8) as _}
      <div class="border border-slate-200/80 rounded-2xl p-4 flex flex-col gap-3 bg-white shadow-sm">
        <div class="w-full h-40 rounded-xl bg-slate-200 animate-pulse"></div>
        <div class="h-4 w-3/4 rounded bg-slate-200 animate-pulse"></div>
        <div class="h-3 w-1/2 rounded bg-slate-200 animate-pulse"></div>
        <div class="h-6 w-1/3 rounded bg-slate-200 animate-pulse"></div>
      </div>
    {/each}
  </div>
{:else}
  <div class="flex flex-col items-center justify-center gap-3 py-20 text-slate-400">
    <svg viewBox="0 0 24 24" width="56" height="56" fill="currentColor"><path d="M19 6h-2c0-2.76-2.24-5-5-5S7 3.24 7 6H5c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-7-3c1.66 0 3 1.34 3 3H9c0-1.66 1.34-3 3-3zm7 17H5V8h14v12z"/></svg>
    <p class="m-0 text-base">Şu an satışta ürün yok.</p>
    {#if arama || secilenKategori}
      <p class="m-0 text-sm">Filtreleri değiştirip tekrar deneyin.</p>
    {/if}
  </div>
{/if}
