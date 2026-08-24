<script>
  import { onMount } from "svelte";
  import { Chart, registerables } from "chart.js";
  import { API, authHeader, durum, sayfalar, sayfala, toplamSayfa, sayfaGit } from "./store.svelte.js";
  Chart.register(...registerables);

  let myMovements = $state([]);
  let history = $state([]);
  let chartCanvas = $state(null);
  let chartInstance = null;

  async function loadMyMovements() {
    try {
      const res = await fetch(`${API}/api/my-stock/movements`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      myMovements = await res.json();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  async function loadHistory() {
    try {
      const res = await fetch(`${API}/api/my-stock/history`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      history = await res.json();
      setTimeout(drawChart, 0);
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  function drawChart() {
    if (!chartCanvas || !history.length) return;
    if (chartInstance) chartInstance.destroy();
    chartInstance = new Chart(chartCanvas, {
      type: "bar",
      data: {
        labels: history.map((h) => new Date(h.tarih).toLocaleDateString("tr-TR")),
        datasets: [
          { label: "Giriş", data: history.map((h) => h.giris), backgroundColor: "#22a722" },
          { label: "Çıkış", data: history.map((h) => h.cikis), backgroundColor: "#c00" },
        ],
      },
      options: { responsive: true, scales: { y: { beginAtZero: true } } },
    });
  }

  onMount(() => {
    loadMyMovements();
    loadHistory();
  });
</script>

<h2>Raporlar</h2>
<div style="display: flex; gap: 2rem; align-items: flex-start; flex-wrap: wrap;">
  <div style="flex: 1; min-width: 280px;">
    <h3>Giriş / Çıkış Geçmişi</h3>
    {#if myMovements.length}
      <div class="tablo-cerceve">
        <table>
          <thead>
            <tr><th>Tarih</th><th>Ürün</th><th>İşlem</th><th>Miktar</th></tr>
          </thead>
          <tbody>
            {#each sayfala(myMovements, "raporlar") as m}
              <tr>
                <td>{new Date(m.created_at).toLocaleDateString("tr-TR")}</td>
                <td>{m.urun}</td>
                <td style="color: {m.quantity > 0 ? 'green' : 'red'}">
                  {m.quantity > 0 ? "Giriş" : "Çıkış"}
                </td>
                <td>{Math.abs(m.quantity)}</td>
              </tr>
            {/each}
          </tbody>
        </table>
        <div class="pagination">
          <button onclick={() => sayfaGit("raporlar", -1)}
                  disabled={(sayfalar.raporlar ?? 1) === 1}>Önceki</button>
          <span>Sayfa {sayfalar.raporlar ?? 1} / {toplamSayfa(myMovements)}</span>
          <button onclick={() => sayfaGit("raporlar", 1)}
                  disabled={(sayfalar.raporlar ?? 1) === toplamSayfa(myMovements)}>Sonraki</button>
        </div>
      </div>
    {:else}
      <p>Henüz giriş/çıkış yapılmamış.</p>
    {/if}
  </div>

  <div style="flex: 1; min-width: 350px;">
    <h3>Giriş / Çıkış Grafiği</h3>
    {#if history.length}
      <div><canvas bind:this={chartCanvas}></canvas></div>
    {/if}
  </div>
</div>