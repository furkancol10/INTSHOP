<script>
  let { acik = $bindable(), onay } = $props();
  let sebep = $state("");

  function kapat() {
    acik = false;
    sebep = "";
  }

  function gonder() {
    onay(sebep);
    kapat();
  }
</script>

{#if acik}
  <div class="modal-arkaplan" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="modal" onclick={(e) => e.stopPropagation()} role="presentation">
      <h3>Talebi Reddet</h3>
      <label>Red Sebebi
        <input bind:value={sebep} placeholder="Örn: Fiyat çok düşük"
               onkeydown={(e) => e.key === "Enter" && gonder()} />
      </label>
      <div class="modal-butonlar">
        <button class="iptal-btn" onclick={kapat}>İptal</button>
        <button class="red-btn" onclick={gonder}>Reddet</button>
      </div>
    </div>
  </div>
{/if}