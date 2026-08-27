<script>
  import { page } from "$app/stores";
</script>

<h2>Bir şeyler ters gitti</h2>
<p class="error">
  {$page.status} - {$page.error?.message ?? "Beklenmeyen bir hata oluştu."}
</p>
<a href="/magaza">Mağazaya dön</a>
