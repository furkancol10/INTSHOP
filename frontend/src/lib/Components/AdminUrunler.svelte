<script>
  import { API, veri, authHeader, durum, fiyatKolon, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";
  import { ozellikAlanlari, ozellikleriAyristir } from "../urunOzellikleri.js";
  import UrunModal from "../Modals/UrunModal.svelte";

  let { yenile } = $props();

  let modalAcik = $state(false);
  let duzenlenenUrun = $state(null);

  function ozellikOzeti(p) {
    const alanlar = ozellikAlanlari(p.category);
    if (!alanlar.length) return "-";
    const degerler = ozellikleriAyristir(p.attributes);
    const dolu = alanlar
      .filter((a) => degerler[a.key])
      .map((a) => `${a.label}: ${degerler[a.key]}`);
    return dolu.length ? dolu.join(" · ") : "-";
  }

  function ekleAc() {
    duzenlenenUrun = null;
    modalAcik = true;
  }

  function duzenleAc(p) {
    duzenlenenUrun = p;
    modalAcik = true;
  }

  async function sil(id) {
    try {
      const res = await fetch(`${API}/api/products/${id}`, {
        method: "DELETE",
        headers: authHeader(),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      yenile();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }
</script>

<h2 class="text-2xl font-semibold mb-4">Ürünler</h2>
<button class="bg-teal-600 text-white border-none px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={ekleAc}>+ Yeni Ürün</button>

{#if veri.products.length}
  <div class="min-h-[530px] overflow-auto border border-white rounded-lg">
    <table class="w-full border-collapse">
      <thead class="bg-coral-500 font-semibold">
        <tr>
          <th class="p-2 border border-slate-100">ID</th><th class="p-2 border border-slate-100">Resim</th><th class="p-2 border border-slate-100">Ürün</th><th class="p-2 border border-slate-100">Kategori</th>
          <th class="p-2 border border-slate-100">Özellikler</th>
          <th class="p-2 border border-slate-100">Toplam Stok</th><th class="p-2 border border-slate-100">Fiyat</th><th class="p-2 border border-slate-100">Aralık</th><th class="p-2 border border-slate-100"></th>
        </tr>
      </thead>
      <tbody>
        {#each sayfala(veri.products, "urunler") as p}
          <tr class="even:bg-yellow-100">
            <td class="p-2 border border-slate-100 text-center">{p.id}</td>
            <td class="p-2 border border-slate-100 text-center">{#if p.image_url}<img src={p.image_url} alt={p.name} class="w-8 h-8 object-cover rounded block" />{/if}</td>
            <td class="p-2 border border-slate-100 text-center">{p.name}</td>
            <td class="p-2 border border-slate-100 text-center">{p.parent_category ? `${p.parent_category} › ${p.category}` : p.category}</td>
            <td class="text-xs text-gray-500 max-w-[220px] truncate p-2 border border-slate-100">{ozellikOzeti(p)}</td>
            <td class="p-2 border border-slate-100 text-center">{p.toplam_stok}</td>
            <td class="p-2 border border-slate-100 text-center">{fiyatKolon(p.price)}</td>
            <td class="text-xs text-gray-500 p-2 border border-slate-100 text-center">{fiyatKolon(p.alt_sinir)} - {fiyatKolon(p.ust_sinir)}</td>
            <td class="p-2 border border-slate-100 text-center">
              <button class="bg-amber-500 text-white border-none px-[.7rem] py-[.3rem] rounded mr-[.3rem] cursor-pointer" onclick={() => duzenleAc(p)}>Düzenle</button>
              <button class="bg-orange-600 w-[30px] rounded-md text-right" onclick={() => sil(p.id)}>Sil</button>
            </td>
          </tr>
        {/each}
      </tbody>
    </table>
  </div>
  <div class="flex items-center gap-4 mt-4">
    <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40" onclick={() => sayfaGit("urunler", -1)}
            disabled={(sayfalar.urunler ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.urunler ?? 1} / {toplamSayfa(veri.products)}</span>
    <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40" onclick={() => sayfaGit("urunler", 1)}
            disabled={(sayfalar.urunler ?? 1) === toplamSayfa(veri.products)}>Sonraki</button>
  </div>
{/if}

<UrunModal bind:acik={modalAcik} urun={duzenlenenUrun} kaydedildi={yenile} />