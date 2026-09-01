<!-- FiltreCubuk.svelte -->
<script>
  let { arama = $bindable(""), placeholder = "Ara...", ekstra } = $props();
</script>

<div class="flex gap-3 items-center">
  <input
    class="flex-1 max-w-[320px] px-[.8rem] py-[.55rem] mb-2 border border-slate-100 rounded-lg text-[.95rem] bg-white focus:outline-none focus:border-teal-600 focus:shadow-[0_0_0_3px_rgba(43,179,163,.15)]"
    bind:value={arama} {placeholder} />
  {@render ekstra?.()}
</div>