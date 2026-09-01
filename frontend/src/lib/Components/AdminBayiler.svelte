<script>
  import { onMount } from "svelte";
  import { API, authHeader, durum, sayfalar, sayfala, toplamSayfa, sayfaGit } from "../store.svelte.js";

  let dealers = $state([]);

  async function loadDealers() {
    try {
      const res = await fetch(`${API}/api/dealers`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      dealers = await res.json();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  onMount(loadDealers);
</script>

<h2 class="text-2xl font-semibold mb-4">Bayiler</h2>
{#if dealers.length}
  <div class="min-h-[530px] overflow-auto border border-white rounded-lg">
    <table class="w-full border-collapse">
      <thead class="bg-coral-500 font-semibold">
        <tr><th class="p-2 border border-slate-100">ID</th><th class="p-2 border border-slate-100">Bayi Adı</th><th class="p-2 border border-slate-100">Adres</th><th class="p-2 border border-slate-100">Telefon</th></tr>
      </thead>
      <tbody>
        {#each sayfala(dealers, "dealers") as d}
          <tr class="even:bg-yellow-100">
            <td class="p-2 border border-slate-100 text-center">{d.id}</td>
            <td class="p-2 border border-slate-100 text-center">{d.username}</td>
            <td class="p-2 border border-slate-100 text-center">{d.address ?? "-"}</td>
            <td class="p-2 border border-slate-100 text-center">{d.phone ?? "-"}</td>
          </tr>
        {/each}
      </tbody>
    </table>
  </div>
  <div class="flex items-center gap-4 mt-4">
    <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40" 
            onclick={() => sayfaGit("dealers", -1)}
            disabled={(sayfalar.dealers ?? 1) === 1}>Önceki</button>
    <span>Sayfa {sayfalar.dealers ?? 1} / {toplamSayfa(dealers)}</span>
    <button class="px-4 py-2 bg-teal-600 text-slate-100 rounded-md disabled:bg-gray-500 disabled:opacity-40" 
            onclick={() => sayfaGit("dealers", 1)}
            disabled={(sayfalar.dealers ?? 1) === toplamSayfa(dealers)}>Sonraki</button>
  </div>
{:else}
  <p>Henüz bayi yok.</p>
{/if}