<script>
  import { onMount } from "svelte";
  import {
    API,
    authHeader,
    durum,
    veri,
    fiyatKolon,
    sepeteEkle,
  } from "../store.svelte.js";

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
    const res = await fetch(`${API}/api/categories`);
    if (res.ok) veri.categories = await res.json();
    await loadShop();
  });
</script>

<h2>Mağaza</h2>
<div class="filtre-cubuk">
  <input bind:value={arama} placeholder="Ürün ara..." />
  <select bind:value={secilenKategori}>
    <option value="">Tüm kategoriler</option>
    {#each veri.categories as k}
      <option value={k.id}>{k.name}</option>
    {/each}
  </select>
</div>

{#if shopData.length}
  <div class="urun-kartlari">
    {#each shopData as urun}
      <div class="urun-kart">
        {#if urun.image_url}
          <img
            src={urun.image_url}
            alt={urun.name}
            class="kart-resim"
            onerror={(e) => (e.currentTarget.src = "/images/placeholder.png")}
          />
        {/if}
        <h3>{urun.name}</h3>
        <p class="kart-satici">Satıcı: <strong>{urun.dealer_name}</strong></p>
        <p class="kart-fiyat">{fiyatKolon(urun.price)}</p>
        <p class="kart-stok">Stok: {urun.stock}</p>
        <button
          class="sepet-btn"
          onclick={async () => {
            try {
              await sepeteEkle(urun.product_id, urun.dealer_id);
              durum.bildirim = "Ürün sepete eklendi";
              setTimeout(() => (durum.bildirim = ""), 2000);
            } catch (e) {
              durum.error = e.message;
            }
          }}>Sepete Ekle</button
        >
      </div>
    {/each}
  </div>
  {#if !hepsiYuklendi}
    <div bind:this={sentinel} class="sentinel">
      {#if yukleniyor}<div class="spinner"></div>{/if}
    </div>
  {/if}
{:else}
  <p>Şu an satışta ürün yok.</p>
{/if}
