<script>
  import { onMount } from "svelte";
  import {
    API,
    authHeader,
    durum,
    fiyatKolon,
    sepeteEkle,
  } from "../store.svelte.js";

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
      const res = await fetch(
        `${API}/api/shop?offset=${shopOffset}&limit=${limit}`,
        {
          headers: authHeader(),
        },
      );
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

  onMount(loadShop);
</script>

<h2>Mağaza</h2>
{#if shopData.length}
  <div class="urun-kartlari">
    {#each shopData as urun}
      <div class="urun-kart">
        {#if urun.image_url}
          <img src={urun.image_url} alt={urun.name} class="kart-resim" />
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
