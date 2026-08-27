<script>
    let {
        acik = $bindable(false),
        baslik = "Emin misiniz ?",
        mesaj = "",
        onayYazi = "Evet",
        iptalYazi = "Vazgeç",
        onaylandi,
    } = $props();

    function kapat() {
        acik = false;
    }

    function onayla() {
        acik = false;
        onaylandi?.();
    }
</script>

{#if acik}
    <div class="modal-arka" onclick={kapat} role="presentation">
        <div class="modal-kutu onay-kutu" onclick={(e) => e.stopPropagation()} role="presentation">
            <h3>{baslik}</h3>
            {#if mesaj}<p class="onay-mesaj">{mesaj}</p>{/if}
            <div class="onay-butonlar">
                <button class="vazgec-btn" onclick={kapat}>{iptalYazi}</button>
                <button class="onay-btn" onclick={onayla}>{onayYazi}</button>
            </div>
        </div>
    </div>
{/if}