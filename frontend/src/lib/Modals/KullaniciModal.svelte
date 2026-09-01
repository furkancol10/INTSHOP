<script>
  import { API, jsonHeader } from "../store.svelte.js";

  let { acik = $bindable(), eklendi } = $props();

  let form = $state({
    username: "",
    password: "",
    role: "Kullanici",
    address: "",
    phone: "",
  });
  let hata = $state("");

  function kapat() {
    acik = false;
    form = { username: "", password: "", role: "Kullanici", address: "", phone: "" };
    hata = "";
  }

  async function ekle() {
    hata = "";
    if (!form.username.trim() || !form.password) {
      hata = "Kullanıcı adı ve şifre zorunlu";
      return;
    }
    try {
      const res = await fetch(`${API}/api/register`, {
        method: "POST",
        headers: jsonHeader(),
        body: JSON.stringify(form),
      });
      if (!res.ok) throw new Error((await res.text()) || `HTTP ${res.status}`);
      kapat();
      eklendi();
    } catch (e) {
      hata = e instanceof Error ? e.message : String(e);
    }
  }
</script>

{#if acik}
  <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-[100]" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="bg-white p-8 rounded-xl w-[320px] flex flex-col gap-[.8rem] shadow-[0_8px_32px_rgba(255,255,255,0.2)]" onclick={(e) => e.stopPropagation()} role="presentation">
      <h3 class="m-0 mb-2">Yeni Kullanıcı</h3>

      <input placeholder="Kullanıcı Adı" bind:value={form.username} />
      <input type="password" placeholder="Şifre" bind:value={form.password} />

      <select bind:value={form.role} class="p-[.6rem] border border-gray-300 rounded-md text-[.8rem]">
        <option value="Kullanici">Kullanıcı</option>
        <option value="Bayi">Bayi</option>
        <option value="Admin">Admin</option>
      </select>

      {#if form.role === "Bayi"}
        <input placeholder="Adres" bind:value={form.address} />
        <input placeholder="Telefon" bind:value={form.phone} />
      {/if}

      {#if hata}<p class="text-red-700 text-sm m-0">{hata}</p>{/if}

      <div class="flex gap-2 justify-end mt-2">
        <button class="bg-white border-none px-4 py-2 rounded-md cursor-pointer" onclick={kapat}>İptal</button>
        <button class="bg-teal-600 text-white border-none px-4 py-2 mb-[.6rem] rounded-md cursor-pointer" onclick={ekle}>Ekle</button>
      </div>
    </div>
  </div>
{/if}