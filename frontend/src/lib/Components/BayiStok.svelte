<script>
  import { onMount } from "svelte";
  import { API, veri, authHeader, durum, fiyatKolon, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";
  import IslemModal from "../Modals/IslemModal.svelte"

  let islemModalAcik = $state(false);
  let islemUrunId = $state("");

  async function loadMyStock() {
    try {
      const res = await fetch(`${API}/api/my-stock`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      veri.myStock = await res.json();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  function islemModalAc(productId = "") {
    islemUrunId = productId;
    islemModalAcik = true;
  }

  function depoDurumu(stok) {
    const s = Number(stok) || 0;
    const yuzde = Math.min(100, Math.round((s / 100) * 100));
    let sinif = "bol";
    let etiket = "Bol";
    if (s === 0) { sinif = "bos"; etiket = "Tükendi"; }
    else if (s <= 10) { sinif = "kritik"; etiket = "Kritik"; }
    else if (s <= 30) { sinif = "az"; etiket = "Az"; }
    else if (s <= 60) { sinif = "normal"; etiket = "Normal"; }
    return { yuzde, sinif, sayi: s, etiket };
  }

  onMount(loadMyStock);
</script>

<h2 class="text-2xl font-semibold mb-4">Stok Yönetimi</h2>
<div class="min-h-[530px] overflow-auto border border-white rounded-lg">
  <button class="p-[.65rem] bg-teal-600 text-white border-none rounded-md text-[.85rem] cursor-pointer transition hover:bg-teal-700 mb-[.6rem] w-[100px]" onclick={() => islemModalAc("")}>Stok/fiyat</button>
  <table class="w-full border-collapse">
    <thead class="bg-coral-500 font-semibold">
      <tr>
        <th class="p-2 border border-slate-100">Ürün-Id</th><th class="p-2 border border-slate-100">Ürün</th><th class="p-2 border border-slate-100">Bayi Fiyatı</th><th class="p-2 border border-slate-100">Kategori</th>
        <th class="p-2 border border-slate-100">Depo Durumu</th><th class="p-2 border border-slate-100">Son Güncelleme Tarihi</th><th class="p-2 border border-slate-100">İşlem</th>
      </tr>
    </thead>
    <tbody>
      {#each sayfala(veri.myStock, "stok") as p}
        {@const d = depoDurumu(p.stock)}
        <tr class="even:bg-yellow-100">
          <td class="p-2 border border-slate-100 text-center">{p.product_id}</td>
          <td class="p-2 border border-slate-100 text-center">{p.name}</td>
          <td class="p-2 border border-slate-100 text-center">{fiyatKolon(p.benim_fiyatim)}</td>
          <td class="p-2 border border-slate-100 text-center">{p.category}</td>
          <td class="p-2 border border-slate-100 text-center">
            <div class="relative w-full max-w-[180px] h-[22px] bg-gray-300 rounded-full overflow-hidden">
              <div class="h-full rounded-full transition-[width] duration-[400ms] ease-[ease] {d.sinif === 'bos' ? 'bg-gray-500' : d.sinif === 'kritik' ? 'bg-red-600' : d.sinif === 'az' ? 'bg-orange-500' : d.sinif === 'normal' ? 'bg-yellow-400' : 'bg-green-600'}" style="width: {d.yuzde}%"></div>
              <span class="absolute inset-0 flex items-center justify-center text-[.75rem] font-bold text-black">{d.sayi}</span>
            </div>
            <span class="text-[.75rem] font-semibold">{d.etiket}</span>
          </td>
          <td class="p-2 border border-slate-100 text-center text-[.8rem] text-gray-500 whitespace-nowrap">
            {p.son_hareket ? new Date(p.son_hareket).toLocaleString("tr-TR") : "-"}
          </td>
          <td class="p-2 border border-slate-100 text-center">
            <button class="p-[.45rem] bg-teal-600 text-white border-none rounded-md text-[.85rem] cursor-pointer transition hover:bg-teal-700" onclick={() => islemModalAc(p.product_id)}>Stok/fiyat</button>
          </td>
        </tr>
      {/each}
    </tbody>
  </table>
</div>
<div class="flex items-center gap-4 mt-4">
  <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40" onclick={() => sayfaGit("stok", -1)}
          disabled={(sayfalar.stok ?? 1) === 1}>Önceki</button>
  <span>Sayfa {sayfalar.stok ?? 1} / {toplamSayfa(veri.myStock)}</span>
  <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40" onclick={() => sayfaGit("stok", 1)}
          disabled={(sayfalar.stok ?? 1) === toplamSayfa(veri.myStock)}>Sonraki</button>
</div>

<IslemModal
  bind:acik={islemModalAcik}
  urunId={islemUrunId}
  tamamlandi={(tur) => {
    loadMyStock();
    if (tur === "fiyat") {
      durum.bildirim = "Fiyat talebiniz onaya gönderildi.";
      setTimeout(() => (durum.bildirim = ""), 4000);
    }
  }}
/>