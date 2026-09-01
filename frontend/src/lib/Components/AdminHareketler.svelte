<script>
  import { API, oturum, durum, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";

  let movements = $state([]);

  async function loadMovements() {
    try {
      const res = await fetch(`${API}/api/movements`, {
        headers: { Authorization: oturum.token },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      movements = await res.json();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  $effect(() => {
    if (durum.aktifSekme === "hareketler") {
      loadMovements();
    }
  });
</script>

<h2 class="text-2xl font-semibold mb-4">Bayi Hareketleri</h2>

{#if movements.length}
  <div class="min-h-[530px] overflow-auto border border-white rounded-lg">
    <table class="w-full border-collapse">
      <thead class="bg-coral-500 font-semibold">
        <tr>
          <th class="p-2 border border-slate-100">Bayi</th>
          <th class="p-2 border border-slate-100">Ürün</th>
          <th class="p-2 border border-slate-100">İşlem</th>
          <th class="p-2 border border-slate-100">Miktar</th>
          <th class="p-2 border border-slate-100">Tarih</th>
        </tr>
      </thead>
      <tbody>
        {#each sayfala(movements, "hareketler") as m}
          <tr class="even:bg-yellow-100">
            <td class="p-2 border border-slate-100 text-center">{m.bayi}</td>
            <td class="p-2 border border-slate-100 text-center">{m.urun}</td>
            <td class="p-2 border border-slate-100 text-center {m.quantity > 0 ? 'text-green-600' : 'text-red-700'}">
              {m.quantity > 0 ? "Giriş" : "Çıkış"}
            </td>
            <td class="p-2 border border-slate-100 text-center">{Math.abs(m.quantity)}</td>
            <td class="p-2 border border-slate-100 text-center">{new Date(m.created_at).toLocaleString("tr-TR")}</td>
          </tr>
        {/each}
      </tbody>
    </table>
  </div>
  <div class="flex items-center gap-4 mt-4">
    <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40"
      onclick={() => sayfaGit("hareketler", -1)}
      disabled={(sayfalar.hareketler ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.hareketler ?? 1} / {toplamSayfa(movements)}</span>
    <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40"
      onclick={() => sayfaGit("hareketler", 1)}
      disabled={(sayfalar.hareketler ?? 1) === toplamSayfa(movements)}>Sonraki</button>
  </div>
{:else}
  <p>Henüz hareket yok.</p>
{/if}