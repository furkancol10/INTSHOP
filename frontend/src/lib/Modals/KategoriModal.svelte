<script>
  import { API, jsonHeader } from "../store.svelte.js";

  let { acik = $bindable(), eklendi } = $props();

  let ad = $state("");
  let hata = $state("");

  function kapat() {
    acik = false;
    ad = "";
    hata = "";
  }

  async function ekle() {
    hata = "";
    if (!ad.trim()) {
      hata = "Kategori adı zorunlu";
      return;
    }
    try {
      const res = await fetch(`${API}/api/categories`, {
        method: "POST",
        headers: jsonHeader(),
        body: JSON.stringify({ name: ad.trim(), parent_id: null }),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      kapat();
      eklendi();
    } catch (e) {
      hata = e instanceof Error ? e.message : String(e);
    }
  }
</script>

{#if acik}
  <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="bg-white p-8 rounded-xl w-[320px] flex flex-col gap-[.8rem] shadow-[0_8px_32px_rgba(255,255,255,0.2)]" onclick={(e) => e.stopPropagation()} role="presentation">
      <h3 class="m-0 mb-2">Yeni Ana Kategori</h3>

      <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">Kategori Adı
        <input bind:value={ad} placeholder="Örn: Kozmetik"
               class="font-normal"
               onkeydown={(e) => e.key === "Enter" && ekle()} />
      </label>

      {#if hata}<p class="text-red-700 text-sm m-0">{hata}</p>{/if}

      <div class="flex gap-2 justify-end mt-2">
        <button class="bg-white border-none px-4 py-2 rounded-md cursor-pointer" onclick={kapat}>İptal</button>
        <button class="bg-teal-600 text-white border-none px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={ekle}>Ekle</button>
      </div>
    </div>
  </div>
{/if}