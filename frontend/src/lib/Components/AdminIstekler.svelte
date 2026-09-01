<script>
  import { onMount } from "svelte";
  import { API, authHeader, jsonHeader, durum, fiyatKolon, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";
  import RedModal from "../Modals/RedModal.svelte";

  let { sayiDegisti } = $props();

  let requests = $state([]);
  let filtre = $state("all");
  let redModalAcik = $state(false);
  let redTalepId = $state(null);

  async function loadRequests() {
    try {
      const res = await fetch(`${API}/api/requests?status=${filtre}`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      requests = await res.json();
      sayiDegisti?.(requests.filter((r) => r.status === "pending").length);
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  async function talepKarar(id, karar, not = "") {
    try {
      const res = await fetch(`${API}/api/requests/${id}/${karar}`, {
        method: "PUT",
        headers: jsonHeader(),
        body: JSON.stringify({ note: not }),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      await loadRequests();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  function filtreDegistir(yeni) {
    filtre = yeni;
    loadRequests();
  }

  onMount(loadRequests);
</script>

<h2 class="text-2xl font-semibold mb-4">Fiyat Değişiklik İstekleri</h2>

<div class="flex gap-2 mb-4">
  <button class="px-[.9rem] py-[.4rem] border-none rounded-md cursor-pointer text-[.85rem] {filtre === "all" ? 'bg-teal-700 text-white' : 'bg-teal-600'}" onclick={() => filtreDegistir("all")}>Hepsi</button>
  <button class="px-[.9rem] py-[.4rem] border-none rounded-md cursor-pointer text-[.85rem] {filtre === "pending" ? 'bg-teal-700 text-white' : 'bg-teal-600'}" onclick={() => filtreDegistir("pending")}>Bekleyen</button>
  <button class="px-[.9rem] py-[.4rem] border-none rounded-md cursor-pointer text-[.85rem] {filtre === "approved" ? 'bg-teal-700 text-white' : 'bg-teal-600'}" onclick={() => filtreDegistir("approved")}>Onaylanan</button>
  <button class="px-[.9rem] py-[.4rem] border-none rounded-md cursor-pointer text-[.85rem] {filtre === "rejected" ? 'bg-teal-700 text-white' : 'bg-teal-600'}" onclick={() => filtreDegistir("rejected")}>Reddedilen</button>
</div>

{#if requests.length}
  <div class="min-h-[530px] overflow-auto border border-white rounded-lg">
    <table class="w-full border-collapse">
      <thead class="bg-coral-500 font-semibold">
        <tr>
          <th class="p-2 border border-slate-100">Bayi</th><th class="p-2 border border-slate-100">Ürün</th><th class="p-2 border border-slate-100">Eski Fiyat</th><th class="p-2 border border-slate-100">Yeni Fiyat</th>
          <th class="p-2 border border-slate-100">Aralık</th><th class="p-2 border border-slate-100">Tarih</th><th class="p-2 border border-slate-100">Durum</th><th class="p-2 border border-slate-100">İşlem</th>
        </tr>
      </thead>
      <tbody>
        {#each sayfala(requests, "istekler") as r}
          <tr class="even:bg-yellow-100">
            <td class="p-2 border border-slate-100 text-center">{r.bayi}</td>
            <td class="p-2 border border-slate-100 text-center">{r.urun}</td>
            <td class="p-2 border border-slate-100 text-center">{fiyatKolon(r.old_price)}</td>
            <td class="p-2 border border-slate-100 text-center"><strong>{fiyatKolon(r.new_price)}</strong></td>
            <td class="p-2 border border-slate-100 text-center text-xs text-gray-500">{fiyatKolon(r.alt_sinir)} - {fiyatKolon(r.ust_sinir)}</td>
            <td class="p-2 border border-slate-100 text-center text-xs text-gray-500">{new Date(r.created_at).toLocaleString("tr-TR")}</td>
            <td class="p-2 border border-slate-100 text-center">
              {#if r.status === "pending"}
                <span class="inline-block px-[.6rem] py-[.2rem] rounded-xl text-xs font-semibold bg-pink-100 text-orange-900">Bekliyor</span>
              {:else if r.status === "approved"}
                <span class="inline-block px-[.6rem] py-[.2rem] rounded-xl text-xs font-semibold bg-pink-100 text-green-600">Onaylandı</span>
              {:else if r.status === "rejected"}
                <span class="inline-block px-[.6rem] py-[.2rem] rounded-xl text-xs font-semibold bg-pink-100 text-red-800">Reddedildi</span>
              {:else}
                <span class="inline-block px-[.6rem] py-[.2rem] rounded-xl text-xs font-semibold">{r.status}</span>
              {/if}
              {#if r.admin_note}<div class="text-xs text-gray-500">{r.admin_note}</div>{/if}
            </td>
            <td class="p-2 border border-slate-100 text-center">
              {#if r.status === "pending"}
                <button class="px-[1.1rem] py-[.55rem] rounded-lg bg-red-700 text-white border-none cursor-pointer" onclick={() => talepKarar(r.id, "approve")}>Onayla</button>
                <button class="bg-red-600 text-white border-none px-[.7rem] py-[.35rem] rounded-md cursor-pointer" onclick={() => { redTalepId = r.id; redModalAcik = true; }}>Reddet</button>
              {:else}
                <span class="text-xs text-gray-500">-</span>
              {/if}
            </td>
          </tr>
        {/each}
      </tbody>
    </table>
  </div>
  <div class="flex items-center gap-4 mt-4">
    <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40"
            onclick={() => sayfaGit("istekler", -1)}
            disabled={(sayfalar.istekler ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.istekler ?? 1} / {toplamSayfa(requests)}</span>
    <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40"
            onclick={() => sayfaGit("istekler", 1)}
            disabled={(sayfalar.istekler ?? 1) === toplamSayfa(requests)}>Sonraki</button>
  </div>
{:else}
  <p>Bu durumda istek yok.</p>
{/if}

<RedModal bind:acik={redModalAcik} onay={(sebep) => talepKarar(redTalepId, "reject", sebep)} />