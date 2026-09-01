<script>
  import { API, veri, jsonHeader } from "../store.svelte.js";
  import { ozellikAlanlari, ozellikleriAyristir, ozellikleriStringify } from "../urunOzellikleri.js";

  let { acik = $bindable(), urun = null, kaydedildi } = $props();

  let form = $state({ name: "", category_id: "", price: "", image_url: "" });
  let attributes = $state({});
  let hata = $state("");

  let duzenlemeMi = $derived(urun !== null);

  let kategoriAdi = $derived(
    veri.categories.find((c) => c.id === Number(form.category_id))?.name ?? "",
  );
  let ozellikAlanlariGecerli = $derived(ozellikAlanlari(kategoriAdi));

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
      attributes = urun ? ozellikleriAyristir(urun.attributes) : {};
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
    if (
      resimYolu &&
      !resimYolu.startsWith("/") &&
      !resimYolu.startsWith("http")
    ) {
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
          attributes: ozellikleriStringify(attributes, kategoriAdi),
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
  <div
    class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]"
    onclick={kapat}
    onkeydown={(e) => e.key === "Escape" && kapat()}
    role="button"
    tabindex="0"
  >
    <div
      class="bg-white p-10 rounded-xl w-[90%] max-w-[700px] min-h-[60vh] flex flex-col gap-[1.2rem] shadow-[0_8px_40px_rgba(0,0,0,0.25)]"
      onclick={(e) => e.stopPropagation()}
      role="presentation"
    >
      <h2 class="m-0">{duzenlemeMi ? "Ürün Düzenle" : "Yeni Ürün"}</h2>

      <label class="flex flex-col gap-[.4rem] font-semibold text-gray-700"
        >Ürün Adı
        <input class="p-[.7rem] border border-gray-300 rounded-lg text-base font-normal" bind:value={form.name} placeholder="Ürün adı" />
      </label>

      <label class="flex flex-col gap-[.4rem] font-semibold text-gray-700"
        >Kategori
        <select class="p-[.7rem] border border-gray-300 rounded-lg text-base font-normal" bind:value={form.category_id}>
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

      <label class="flex flex-col gap-[.4rem] font-semibold text-gray-700"
        >Fiyat (₺)
        <input
          class="p-[.7rem] border border-gray-300 rounded-lg text-base font-normal"
          type="number"
          step="0.01"
          bind:value={form.price}
          placeholder="0.00"
        />
      </label>

      <label class="flex flex-col gap-[.4rem] font-semibold text-gray-700">
        Görsel URL
        <input
          class="p-[.7rem] border border-gray-300 rounded-lg text-base font-normal"
          bind:value={form.image_url}
          placeholder="https://upload.wikimedia.org/..."
        />
      </label>

      {#if form.image_url}
        <img
          src={form.image_url}
          alt="önizleme"
          class="w-[120px] h-[120px] object-contain border border-gray-200 rounded-lg mt-2"
          onerror={(e) => (e.currentTarget.style.display = "none")}
          onload={(e) => (e.currentTarget.style.display = "block")}
        />
      {/if}

      {#if ozellikAlanlariGecerli.length}
        <h3 class="m-0 text-base text-gray-700">{kategoriAdi} Özellikleri</h3>
        <div class="grid grid-cols-[repeat(auto-fill,minmax(180px,1fr))] gap-[.9rem]">
          {#each ozellikAlanlariGecerli as alan}
            <label class="flex flex-col gap-[.4rem] font-semibold text-gray-700"
              >{alan.label}
              <input class="p-[.7rem] border border-gray-300 rounded-lg text-base font-normal" bind:value={attributes[alan.key]} placeholder={alan.label} />
            </label>
          {/each}
        </div>
      {/if}

      {#if hata}<p class="text-red-700 text-[.85rem] m-0">{hata}</p>{/if}

      <div class="flex gap-2 justify-end mt-auto">
        <button class="bg-white border-0 px-4 py-2 rounded-md cursor-pointer" onclick={kapat}>İptal</button>
        <button class="bg-teal-600 text-white border-0 px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={kaydet}
          >{duzenlemeMi ? "Kaydet" : "Ekle"}</button
        >
      </div>
    </div>
  </div>
{/if}
