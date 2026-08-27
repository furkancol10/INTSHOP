<script>
  import { oturum } from "$lib/store.svelte.js";
</script>

<div class="hosgeldin">
  <h1>Hoş Geldiniz, {oturum.currentUser}!</h1>
  <p>
    Stok işlemleriniz için "Stok", raporlarınız için "Raporlar" sekmesini
    kullanın.
  </p>
</div>
