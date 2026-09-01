<script>
  import { veri } from "../store.svelte.js";
  import KategoriModal from "../Modals/KategoriModal.svelte";
  import KategoriDetayModal from "../Modals/KategoriDetayModal.svelte";

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

<div class="flex items-center justify-between mb-[1.2rem]">
  <h2 class="text-2xl font-semibold">Kategoriler</h2>
  <button class="bg-teal-600 text-white border-none px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={() => (modalAcik = true)}>+ Yeni Ana Kategori</button>
</div>

{#if anaKategoriler.length}
  <div class="grid grid-cols-[repeat(auto-fill,minmax(260px,1fr))] gap-[1.2rem]">
    {#each anaKategoriler as ana}
      {@const altlar = altlariGetir(ana.id)}
      <button class="bg-sky-100 border border-gray-500 rounded-xl p-[1.2rem] text-center cursor-pointer [font-family:inherit] [font-size:inherit] transition-all duration-200 flex flex-col gap-[.8rem] hover:border-teal-600 hover:shadow-[0_9px_16px_rgba(255,255,255,0.08)] hover:scale-110 active:scale-[.99]" onclick={() => detayAc(ana)}>
        <div class="flex items-center justify-between">
          <h3 class="m-0 text-[1.1rem]">{ana.name}</h3>
          <span class="bg-teal-600 text-white rounded-xl px-[.55rem] py-[.15rem] text-xs font-bold">{altlar.length}</span>
        </div>
        <div class="flex flex-wrap gap-[.4rem]">
          {#if altlar.length}
            {#each altlar.slice(0, 4) as alt}
              <span class="bg-amber-300 text-black px-[.6rem] py-[.25rem] rounded-md text-[.8rem]">{alt.name}</span>
            {/each}
            {#if altlar.length > 4}
              <span class="bg-slate-100 text-gray-500 px-[.6rem] py-[.25rem] rounded-md text-[.8rem] italic">+{altlar.length - 4} daha</span>
            {/if}
          {:else}
            <span class="text-gray-500 text-[.85rem]">Alt Kategori Yok</span>
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