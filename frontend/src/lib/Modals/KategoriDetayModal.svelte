<script>
  import { API, veri, jsonHeader, authHeader } from "../store.svelte.js";

  let { acik = $bindable(), kategori, degisti } = $props();

  let yeniAd = $state("");
  let hata = $state("");

  let altlar = $derived(
    kategori ? veri.categories.filter((c) => c.parent_id === kategori.id) : []
  );

  function kapat() {
    acik = false;
    yeniAd = "";
    hata = "";
  }

  async function ekle() {
    hata = "";
    if (!yeniAd.trim()) {
      hata = "Kategori adı zorunlu";
      return;
    }
    try {
      const res = await fetch(`${API}/api/categories`, {
        method: "POST",
        headers: jsonHeader(),
        body: JSON.stringify({ name: yeniAd.trim(), parent_id: kategori.id }),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      yeniAd = "";
      degisti();
    } catch (e) {
      hata = e instanceof Error ? e.message : String(e);
    }
  }

  async function sil(id) {
    hata = "";
    try {
      const res = await fetch(`${API}/api/categories/${id}`, {
        method: "DELETE",
        headers: authHeader(),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      degisti();
    } catch (e) {
      hata = e instanceof Error ? e.message : String(e);
    }
  }
</script>

{#if acik && kategori}
  <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="bg-slate-100 p-10 rounded-xl w-[90%] max-w-[700px] min-h-[60vh] flex flex-col gap-[1.2rem] shadow-[0_8px_40px_rgba(0,0,0,0.25)]" onclick={(e) => e.stopPropagation()} role="presentation">
      <h2>{kategori.name}</h2>
      <p class="m-0 text-sm bg-slate-100 text-gray-500 text-left">Alt kategoriler</p>

      <div class="flex flex-col gap-[.4rem] max-h-[300px] overflow-y-auto">
        {#each altlar as alt}
          <div class="flex items-center justify-between px-[.8rem] py-[.6rem] bg-sky-100 rounded-lg">
            <span>{alt.name}</span>
            <button class="rounded-[5px] text-right bg-orange-600 w-[30px]" onclick={() => sil(alt.id)}>Sil</button>
          </div>
        {:else}
          <p class="text-gray-500 text-[.85rem]">Bu kategoride alt kategori yok.</p>
        {/each}
      </div>

      <div class="flex gap-2 mt-2">
        <input bind:value={yeniAd} placeholder="Yeni alt kategori adı"
               class="flex-1 p-[.6rem] border border-teal-600 rounded-md"
               onkeydown={(e) => e.key === "Enter" && ekle()} />
        <button class="bg-teal-600 text-white border-none px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={ekle}>Ekle</button>
      </div>

      {#if hata}<p class="text-red-700 text-sm m-0">{hata}</p>{/if}

      <div class="flex gap-2 justify-end mt-2">
        <button class="bg-white border-none px-4 py-2 rounded-md cursor-pointer" onclick={kapat}>Kapat</button>
      </div>
    </div>
  </div>
{/if}