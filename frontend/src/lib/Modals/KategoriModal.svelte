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
  <div class="modal-arkaplan" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="modal" onclick={(e) => e.stopPropagation()} role="presentation">
      <h3>Yeni Ana Kategori</h3>

      <label>Kategori Adı
        <input bind:value={ad} placeholder="Örn: Kozmetik"
               onkeydown={(e) => e.key === "Enter" && ekle()} />
      </label>

      {#if hata}<p class="error">{hata}</p>{/if}

      <div class="modal-butonlar">
        <button class="iptal-btn" onclick={kapat}>İptal</button>
        <button class="ekle-btn" onclick={ekle}>Ekle</button>
      </div>
    </div>
  </div>
{/if}