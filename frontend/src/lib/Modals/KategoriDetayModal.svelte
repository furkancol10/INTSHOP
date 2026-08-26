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
  <div class="modal-arkaplan" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="kategori-detay-modal" onclick={(e) => e.stopPropagation()} role="presentation">
      <h2>{kategori.name}</h2>
      <p class="modal-bilgi">Alt kategoriler</p>

      <div class="alt-liste">
        {#each altlar as alt}
          <div class="alt-satir">
            <span>{alt.name}</span>
            <button class="sil" onclick={() => sil(alt.id)}>Sil</button>
          </div>
        {:else}
          <p class="bos-yazi">Bu kategoride alt kategori yok.</p>
        {/each}
      </div>

      <div class="alt-ekle-satir">
        <input bind:value={yeniAd} placeholder="Yeni alt kategori adı"
               onkeydown={(e) => e.key === "Enter" && ekle()} />
        <button class="ekle-btn" onclick={ekle}>Ekle</button>
      </div>

      {#if hata}<p class="error">{hata}</p>{/if}

      <div class="modal-butonlar">
        <button class="iptal-btn" onclick={kapat}>Kapat</button>
      </div>
    </div>
  </div>
{/if}