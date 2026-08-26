<script>
  import { API, veri, jsonHeader } from "../store.svelte.js";

  let { acik = $bindable(), urun = null, kaydedildi } = $props();

  let form = $state({ name: "", category_id: "", price: "", image_url: "" });
  let hata = $state("");

  let duzenlemeMi = $derived(urun !== null);

  $effect(() => {
    if (acik) {
      form = urun
        ? {
            name: urun.name,
            category_id: urun.category_id ?? "",
            price: urun.price,
            image_url: urun.image_url ?? "",
          }
        : { name: "", category_id: "", price: "", image_url: "" };
      hata = "";
    }
  });

  let onizlemeYolu = $derived.by(() => {
    let v = form.image_url?.trim() || "";
    if (!v) return "";
    if (v.startsWith("/") || v.startsWith("http")) return v;
    if (!/\.(jpg|jpeg|png|webp|gif)$/i.test(v)) v += ".jpg";
    return `/products/${v}`;
  });

  function kapat() {
    acik = false;
    hata = "";
  }

  async function kaydet() {
    hata = "";
    if (!form.name.trim()) {
      hata = "Ürün adı zorunlu";
      return;
    }
    if (!form.category_id) {
      hata = "Kategori seçin";
      return;
    }

    const url = duzenlemeMi
      ? `${API}/api/products/${urun.id}`
      : `${API}/api/products`;
    const method = duzenlemeMi ? "PUT" : "POST";

    let resimYolu = form.image_url?.trim() || "";
    if (resimYolu && !resimYolu.startsWith("/") && !resimYolu.startsWith("http")) {
      if (!/\.(jpg|jpeg|png|webp|gif)$/i.test(resimYolu)) resimYolu += ".jpg";
      resimYolu = `/products/${resimYolu}`;
    }

    try {
      const res = await fetch(url, {
        method,
        headers: jsonHeader(),
        body: JSON.stringify({
          name: form.name.trim(),
          category_id: Number(form.category_id),
          stock: 0,
          price: Number(form.price),
          image_url: resimYolu,
        }),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      kapat();
      kaydedildi();
    } catch (e) {
      hata = e instanceof Error ? e.message : String(e);
    }
  }
</script>

{#if acik}
  <div class="modal-arkaplan" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="buyuk-modal" onclick={(e) => e.stopPropagation()} role="presentation">
      <h2>{duzenlemeMi ? "Ürün Düzenle" : "Yeni Ürün"}</h2>

      <label>Ürün Adı
        <input bind:value={form.name} placeholder="Ürün adı" />
      </label>

      <label>Kategori
        <select bind:value={form.category_id}>
          <option value="">Kategori seç</option>
          {#each veri.categories.filter((c) => !c.parent_id) as ust}
            {#if veri.categories.some((c) => c.parent_id === ust.id)}
              <optgroup label={ust.name}>
                {#each veri.categories.filter((c) => c.parent_id === ust.id) as alt}
                  <option value={alt.id}>{alt.name}</option>
                {/each}
              </optgroup>
            {:else}
              <option value={ust.id}>{ust.name}</option>
            {/if}
          {/each}
        </select>
      </label>

      <label>Fiyat (₺)
        <input type="number" step="0.01" bind:value={form.price} placeholder="0.00" />
      </label>

      <label>Resim
        <input bind:value={form.image_url} placeholder="Ürün adı" />
      </label>

      {#if onizlemeYolu}
        <img src={onizlemeYolu} alt="Önizleme" style="max-width: 150px; border-radius: 8px;" />
      {/if}

      {#if hata}<p class="error">{hata}</p>{/if}

      <div class="modal-butonlar">
        <button class="iptal-btn" onclick={kapat}>İptal</button>
        <button class="ekle-btn" onclick={kaydet}>{duzenlemeMi ? "Kaydet" : "Ekle"}</button>
      </div>
    </div>
  </div>
{/if}