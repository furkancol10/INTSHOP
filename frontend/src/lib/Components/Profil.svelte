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

<h2 class="text-2xl font-semibold mb-4">Profilim</h2>
<div class="flex gap-8 items-start flex-wrap max-w-[600px]">
  <div>
    {#if profil.avatar_url}<img src={profil.avatar_url} alt="avatar" class="w-[150px] h-[150px] rounded-full object-cover border-[3px] border-slate-100" />{/if}
  </div>

  <div class="flex-1 min-w-[250px] flex flex-col gap-[0.8rem]">
    <label class="flex flex-col gap-[0.3rem] font-semibold text-gray-400">Kullanıcı Adı <input value={profil.username} disabled class="p-[0.6rem] border border-slate-100 rounded-md font-normal disabled:bg-white disabled:text-gray-500" /></label>
    <label class="flex flex-col gap-[0.3rem] font-semibold text-gray-400">Rol <input value={profil.role} disabled class="p-[0.6rem] border border-slate-100 rounded-md font-normal disabled:bg-white disabled:text-gray-500" /></label>
    <label class="flex flex-col gap-[0.3rem] font-semibold text-gray-400">Adres <input bind:value={profil.address} placeholder="Adres" class="p-[0.6rem] border border-slate-100 rounded-md font-normal disabled:bg-white disabled:text-gray-500" /></label>
    <label class="flex flex-col gap-[0.3rem] font-semibold text-gray-400">Telefon <input bind:value={profil.phone} placeholder="Telefon" class="p-[0.6rem] border border-slate-100 rounded-md font-normal disabled:bg-white disabled:text-gray-500" /></label>
    <label class="flex flex-col gap-[0.3rem] font-semibold text-gray-400">Avatar URL <input bind:value={profil.avatar_url} placeholder="/avatars/..." class="p-[0.6rem] border border-slate-100 rounded-md font-normal disabled:bg-white disabled:text-gray-500" /></label>

    {#if mesaj}<p class="text-green-600 text-[0.85rem] m-0 text-center">{mesaj}</p>{/if}

    <button class="bg-teal-600 text-white border-none px-4 py-2 mb-[0.6rem] rounded-md cursor-pointer" onclick={guncelle}>Kaydet</button>
  </div>
</div>