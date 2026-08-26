<script>
  import { onMount } from "svelte";
  import { API, oturum, durum, authHeader, jsonHeader } from "../store.svelte.js";

  let profil = $state({
    username: "", role: "", address: "", phone: "", avatar_url: "",
  });
  let mesaj = $state("");

  async function loadProfile() {
    try {
      const res = await fetch(`${API}/api/profile`, { headers: authHeader() });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      profil = await res.json();
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  async function guncelle() {
    mesaj = "";
    try {
      const res = await fetch(`${API}/api/profile`, {
        method: "PUT",
        headers: jsonHeader(),
        body: JSON.stringify({
          address: profil.address,
          phone: profil.phone,
          avatar_url: profil.avatar_url,
        }),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      oturum.avatarUrl = profil.avatar_url || "";
      localStorage.setItem("avatar_url", oturum.avatarUrl);
      mesaj = "Profil güncellendi";
      setTimeout(() => (mesaj = ""), 3000);
    } catch (e) {
      durum.error = e instanceof Error ? e.message : String(e);
    }
  }

  onMount(loadProfile);
</script>

<h2>Profilim</h2>
<div class="profil-sayfa">
  <div class="profil-avatar">
    {#if profil.avatar_url}<img src={profil.avatar_url} alt="avatar" />{/if}
  </div>

  <div class="profil-bilgi">
    <label>Kullanıcı Adı <input value={profil.username} disabled /></label>
    <label>Rol <input value={profil.role} disabled /></label>
    <label>Adres <input bind:value={profil.address} placeholder="Adres" /></label>
    <label>Telefon <input bind:value={profil.phone} placeholder="Telefon" /></label>
    <label>Avatar URL <input bind:value={profil.avatar_url} placeholder="/avatars/..." /></label>

    {#if mesaj}<p class="basari">{mesaj}</p>{/if}

    <button class="ekle-btn" onclick={guncelle}>Kaydet</button>
  </div>
</div>