<script>
  import { durum, sepet, fiyatKolon } from "../store.svelte.js";

  function ac() {
    durum.sepetPopup = !durum.sepetPopup;
  }

  function kapat() {
    durum.sepetPopup = false;
  }

  function sepeteGit() {
    durum.aktifSekme = "sepet";
    durum.sepetPopup = false;
  }
</script>

<div class="sepet-widget">
  <button class="sepet-widget-btn" onclick={ac} aria-label="Sepet">
    <svg viewBox="0 0 24 24" width="22" height="22" fill="white">
      <path
        d="M7 18c-1.1 0-1.99.9-1.99 2S5.9 22 7 22s2-.9 2-2-.9-2-2-2zM1 2v2h2l3.6 7.59-1.35 2.45c-.16.28-.25.61-.25.96 0 1.1.9 2 2 2h12v-2H7.42c-.14 0-.25-.11-.25-.25l.03-.12L8.1 13h7.45c.75 0 1.41-.41 1.75-1.03l3.58-6.49A.996.996 0 0 0 20 4H5.21l-.94-2H1zm16 16c-1.1 0-1.99.9-1.99 2s.89 2 1.99 2 2-.9 2-2-.9-2-2-2z"
      />
    </svg>
    {#if sepet.adet > 0}
      <span class="sepet-widget-rozet">{sepet.adet}</span>
    {/if}
  </button>

  {#if durum.sepetPopup}
    <div
      class="sepet-widget-overlay"
      onclick={kapat}
      role="presentation"
    ></div>
    <div class="sepet-widget-popup">
      <h4>Sepetim</h4>
      {#if sepet.satirlar.length}
        <div class="sepet-widget-liste">
          {#each sepet.satirlar as s}
            <div class="sepet-widget-satir">
              {#if s.image_url}
                <img
                  src={s.image_url}
                  alt={s.urun}
                  onerror={(e) => (e.currentTarget.src = "/images/placeholder.png")}
                />
              {:else}
                <div class="sepet-widget-gorsel-yok"></div>
              {/if}
              <div class="sepet-widget-bilgi">
                <span class="sepet-widget-ad">{s.urun}</span>
                <span class="sepet-widget-adet">{s.quantity} adet</span>
              </div>
              <span class="sepet-widget-fiyat">{fiyatKolon(s.satir_tutar)}</span>
            </div>
          {/each}
        </div>
        <div class="sepet-widget-toplam">
          <span>Toplam</span>
          <strong>{fiyatKolon(sepet.toplam)}</strong>
        </div>
        <button class="sepet-widget-git" onclick={sepeteGit}>Sepete Git</button>
      {:else}
        <p class="sepet-widget-bos">Sepetiniz boş.</p>
      {/if}
    </div>
  {/if}
</div>
