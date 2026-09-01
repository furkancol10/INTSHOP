<script>
  import { API, veri, jsonHeader, fiyatKolon } from "../store.svelte.js";

  let { acik = $bindable(), urunId = "", tamamlandi } = $props();

  let secilenId = $state("");
  let islemTuru = $state("");
  let miktar = $state("");
  let yeniFiyat = $state("");
  let hata = $state("");

  $effect(() => {
    if (acik) {
      secilenId = urunId;
      islemTuru = "";
      miktar = "";
      yeniFiyat = "";
      hata = "";
    }
  });

  let secilenUrun = $derived(
    veri.myStock.find((p) => p.product_id === Number(secilenId)) ?? null
  );

  let fiyatDurum = $derived.by(() => {
    if (islemTuru !== "fiyat" || !secilenUrun || !yeniFiyat) return null;
    const f = Number(yeniFiyat);
    if (isNaN(f)) return null;
    const alt = Number(secilenUrun.alt_sinir);
    const ust = Number(secilenUrun.ust_sinir);
    if (f < alt || f > ust) return "disarida";
    const aralik = ust - alt;
    if (f - alt < aralik * 0.15 || ust - f < aralik * 0.15) return "sinirda";
    return "iyi";
  });

  function kapat() {
    acik = false;
    hata = "";
  }

  async function kaydet() {
    hata = "";
    if (!secilenId) {
      hata = "Ürün seçin";
      return;
    }
    if (!islemTuru) {
      hata = "İşlem türü seçin";
      return;
    }

    const pid = Number(secilenId);

    if (islemTuru === "fiyat") {
      const f = Number(yeniFiyat);
      if (!yeniFiyat || isNaN(f) || f <= 0) {
        hata = "Geçerli bir fiyat girin";
        return;
      }
      if (fiyatDurum === "disarida") {
        hata = `Fiyat ${fiyatKolon(secilenUrun.alt_sinir)} - ${fiyatKolon(secilenUrun.ust_sinir)} aralığında olmalı`;
        return;
      }
      try {
        const res = await fetch(`${API}/api/my-stock/price`, {
          method: "PUT",
          headers: jsonHeader(),
          body: JSON.stringify({ product_id: pid, price: f }),
        });
        if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
        kapat();
        tamamlandi("fiyat");
      } catch (e) {
        hata = e instanceof Error ? e.message : String(e);
      }
    } else {
      const m = Math.abs(Number(miktar) || 0);
      if (m <= 0) {
        hata = "Miktar 0'dan büyük olmalı";
        return;
      }
      const degisim = islemTuru === "giris" ? m : -m;
      try {
        const res = await fetch(`${API}/api/my-stock/movement`, {
          method: "POST",
          headers: jsonHeader(),
          body: JSON.stringify({ product_id: pid, change: degisim }),
        });
        if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
        kapat();
        tamamlandi("hareket");
      } catch (e) {
        hata = e instanceof Error ? e.message : String(e);
      }
    }
  }
</script>

{#if acik}
  <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="bg-white p-8 rounded-xl w-80 flex flex-col gap-[.8rem] shadow-[0_8px_32px_rgba(255,255,255,0.2)]" onclick={(e) => e.stopPropagation()} role="presentation">
      <h3 class="m-0 mb-2">Stok-Fiyat Güncelle</h3>

      <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">Ürün
        <select class="font-normal p-[.6rem] border border-gray-300 rounded-md text-[.8rem]" bind:value={secilenId}>
          <option value="">Ürün Seçiniz</option>
          {#each veri.myStock as p}
            <option value={p.product_id}>{p.name}</option>
          {/each}
        </select>
      </label>

      <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">İşlem Türü
        <select class="font-normal p-[.6rem] border border-gray-300 rounded-md text-[.8rem]" bind:value={islemTuru}>
          <option value="">İşlem türü seçiniz</option>
          <option value="giris">Giriş</option>
          <option value="cikis">Çıkış</option>
          <option value="fiyat">Fiyat Güncelleme</option>
        </select>
      </label>

      {#if secilenUrun && (islemTuru === "giris" || islemTuru === "cikis")}
        <p class="m-0 text-sm bg-slate-100 text-gray-500 text-left">Mevcut stok: <strong>{secilenUrun.stock}</strong></p>
        <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">Miktar
          <input class="font-normal" type="number" min="1" bind:value={miktar} placeholder="0" />
        </label>

      {:else if secilenUrun && islemTuru === "fiyat"}
        <p class="m-0 text-sm bg-slate-100 text-gray-500 text-left">
          Mevcut fiyat: <strong>{fiyatKolon(secilenUrun.benim_fiyatim)}</strong>
        </p>
        <label class="flex flex-col gap-[.3rem] font-semibold text-gray-400">Yeni Fiyat
          <input type="number" step="0.01" bind:value={yeniFiyat}
                 placeholder={secilenUrun.onerilen}
                 class="font-normal p-[.4rem] border-2 rounded-md {fiyatDurum === 'iyi' ? 'border-green-600' : fiyatDurum === 'sinirda' ? 'border-orange-500' : fiyatDurum === 'disarida' ? 'border-red-600' : 'border-slate-100'}" />
        </label>
        <p class="text-xs text-gray-500 mt-[.3rem]">
          Aralık: {fiyatKolon(secilenUrun.alt_sinir)} - {fiyatKolon(secilenUrun.ust_sinir)}
          · <button type="button"
                    onclick={() => (yeniFiyat = secilenUrun.onerilen)}>
              Önerilen: {fiyatKolon(secilenUrun.onerilen)}
            </button>
        </p>
      {/if}

      {#if hata}<p class="text-red-700 text-[.85rem] m-0">{hata}</p>{/if}

      <div class="flex gap-2 justify-end mt-2">
        <button class="bg-white border-0 px-4 py-2 rounded-md cursor-pointer" onclick={kapat}>İptal</button>
        <button class="bg-teal-600 text-white border-0 px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={kaydet}>Kaydet</button>
      </div>
    </div>
  </div>
{/if}