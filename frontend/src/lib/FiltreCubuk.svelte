<!-- FiltreCubuk.svelte -->
<script>
  let { arama = $bindable(""), placeholder = "Ara...", ekstra } = $props();
</script>

<div class="filtre-cubuk">
  <input bind:value={arama} {placeholder} />
  {@render ekstra?.()}
</div>