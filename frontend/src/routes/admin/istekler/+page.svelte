<script>
  import AdminIstekler from "$lib/Components/AdminIstekler.svelte";
  import { durum } from "$lib/store.svelte.js";
</script>

<AdminIstekler sayiDegisti={(n) => (durum.bekleyenIstekSayisi = n)} />
