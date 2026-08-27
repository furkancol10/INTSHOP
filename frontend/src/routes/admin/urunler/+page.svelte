<script>
  import AdminUrunler from "$lib/Components/AdminUrunler.svelte";
  import { loadAdminVeri } from "$lib/store.svelte.js";
</script>

<AdminUrunler yenile={loadAdminVeri} />
