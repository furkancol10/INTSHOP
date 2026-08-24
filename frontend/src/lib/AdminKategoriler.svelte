<script>
  import { veri } from "./store.svelte.js";
  import KategoriModal from "./KategoriModal.svelte";
  import KategoriDetayModal from "./KategoriDetayModal.svelte";

  let { yenile } = $props();

  let modalAcik = $state(false);
  let detayAcik = $state(false);
  let acikKategori = $state(null);

  let anaKategoriler = $derived(veri.categories.filter((c) => !c.parent_id));

  function altlariGetir(anaId) {
    return veri.categories.filter((c) => c.parent_id === anaId);
  }

  function detayAc(kategori) {
    acikKategori = kategori;
    detayAcik = true;
  }
</script>

<div class="sekme-baslik">
  <h2>Kategoriler</h2>
  <button class="ekle-btn" onclick={() => (modalAcik = true)}>+ Yeni Ana Kategori</button>
</div>

{#if anaKategoriler.length}
  <div class="kategori-kartlari">
    {#each anaKategoriler as ana}
      {@const altlar = altlariGetir(ana.id)}
      <button class="kategori-kart" onclick={() => detayAc(ana)}>
        <div class="kart-ust">
          <h3>{ana.name}</h3>
          <span class="kart-sayi">{altlar.length}</span>
        </div>
        <div class="kart-altlar">
          {#if altlar.length}
            {#each altlar.slice(0, 4) as alt}
              <span class="alt-etiket">{alt.name}</span>
            {/each}
            {#if altlar.length > 4}
              <span class="alt-etiket daha">+{altlar.length - 4} daha</span>
            {/if}
          {:else}
            <span class="bos-yazi">Alt Kategori Yok</span>
          {/if}
        </div>
      </button>
    {/each}
  </div>
{:else}
  <p>Kategori Yok</p>
{/if}

<KategoriModal bind:acik={modalAcik} eklendi={yenile} />
<KategoriDetayModal bind:acik={detayAcik} kategori={acikKategori} degisti={yenile} />