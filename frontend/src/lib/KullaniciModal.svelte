<script>
  import { API, jsonHeader } from "./store.svelte.js";

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
  <div class="modal-arkaplan" onclick={kapat}
       onkeydown={(e) => e.key === "Escape" && kapat()}
       role="button" tabindex="0">
    <div class="modal" onclick={(e) => e.stopPropagation()} role="presentation">
      <h3>Yeni Kullanıcı</h3>

      <input placeholder="Kullanıcı Adı" bind:value={form.username} />
      <input type="password" placeholder="Şifre" bind:value={form.password} />

      <select bind:value={form.role}>
        <option value="Kullanici">Kullanıcı</option>
        <option value="Bayi">Bayi</option>
        <option value="Admin">Admin</option>
      </select>

      {#if form.role === "Bayi"}
        <input placeholder="Adres" bind:value={form.address} />
        <input placeholder="Telefon" bind:value={form.phone} />
      {/if}

      {#if hata}<p class="error">{hata}</p>{/if}

      <div class="modal-butonlar">
        <button class="iptal-btn" onclick={kapat}>İptal</button>
        <button class="ekle-btn" onclick={ekle}>Ekle</button>
      </div>
    </div>
  </div>
{/if}