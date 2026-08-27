<script>
  import AdminKategoriler from "$lib/Components/AdminKategoriler.svelte";
  import { loadAdminVeri } from "$lib/store.svelte.js";
</script>

<AdminKategoriler yenile={loadAdminVeri} />
