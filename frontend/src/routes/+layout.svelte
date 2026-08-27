<script>
  import "../app.css";
  import { onMount } from "svelte";
  import { goto } from "$app/navigation";
  import { page } from "$app/stores";
  import { API, oturum, durum, sepet, sepetYukle, loadAdminVeri } from "$lib/store.svelte.js";
  import Login from "$lib/Components/Login.svelte";

  let { children } = $props();

  let profilYukleniyor = $state(false);

  let anaSayfa = $derived(
    oturum.role === "Kullanici"
      ? "/magaza"
      : oturum.role === "Bayi"
        ? "/bayi"
        : oturum.role === "Admin"
          ? "/admin/loglar"
          : "/",
  );

  // Her rolun girebilecegi yol on-eki - dogrudan URL ile baska rolun
  // sayfasina girilmeye calisilirsa buradan reddedilir.
  function yolYetkiliMi(pathname, role) {
    if (role === "Kullanici") return pathname.startsWith("/magaza") || pathname.startsWith("/sepet");
    if (role === "Bayi") return pathname.startsWith("/bayi");
    if (role === "Admin") return pathname.startsWith("/admin");
    return false;
  }

  async function girisSonrasi() {
    await goto(anaSayfa);
  }

  async function logout() {
    try {
      await fetch(`${API}/api/logout`, {
        method: "POST",
        headers: { Authorization: oturum.token },
      });
    } catch {
      // sunucuya ulasilamasa bile yerel cikis yapilmali
    }

    oturum.token = "";
    oturum.role = "";
    oturum.currentUser = "";
    oturum.avatarUrl = "";
    localStorage.clear();
    goto("/");
  }

  onMount(async () => {
    if (!oturum.token) return;
    profilYukleniyor = true;
    try {
      const res = await fetch(`${API}/api/profile`, {
        headers: { Authorization: oturum.token },
      });
      if (!res.ok) {
        logout();
        return;
      }
      const p = await res.json();
      oturum.role = p.role;
      oturum.currentUser = p.username;
      oturum.avatarUrl = p.avatar_url || "";
      localStorage.setItem("username", oturum.currentUser);
      localStorage.setItem("avatar_url", oturum.avatarUrl);
      if (oturum.role === "Kullanici") sepetYukle();
      else if (oturum.role === "Admin") loadAdminVeri();
    } catch {
      logout();
    } finally {
      profilYukleniyor = false;
    }
  });

  // Rol belli olduktan sonra: kok yoldaysak ya da bulunulan yol
  // o rolun alani disindaysa (ör. Kullanici, /admin/... yazip girmeye
  // calisirsa) kendi ana sayfasina geri gonder. $page.url.pathname veya
  // rol degistikce yeniden calisir.
  $effect(() => {
    if (!oturum.token || !oturum.role) return;
    const yol = $page.url.pathname;
    if (yol === "/" || !yolYetkiliMi(yol, oturum.role)) {
      goto(anaSayfa, { replaceState: true });
    }
  });
</script>

<main>
  {#if !oturum.token}
    <Login girisYapildi={girisSonrasi} />
  {:else if profilYukleniyor || !oturum.role}
    <div class="sentinel"><div class="spinner"></div></div>
  {:else}
    <div class="toolbar">
      <div class="toolbar-ic">
        <a class="toolbar-baslik" href={anaSayfa}>INTSHOP <small>(SvelteKit deneme)</small></a>
        <div class="toolbar-sekmeler">
          {#if oturum.role === "Kullanici"}
            <a href="/magaza">Mağaza</a>
            <a href="/sepet">Sepetim{#if sepet.adet > 0} ({sepet.adet}){/if}</a>
          {:else if oturum.role === "Bayi"}
            <a href="/bayi/stok">Stok</a>
            <a href="/bayi/raporlar">Raporlar</a>
            <a href="/bayi/talepler">Talepler</a>
          {:else if oturum.role === "Admin"}
            <a href="/admin/dealers">Bayiler</a>
            <a href="/admin/urunler">Ürünler</a>
            <a href="/admin/kategoriler">Kategoriler</a>
            <a href="/admin/istekler"
              >İstekler{#if durum.bekleyenIstekSayisi > 0} ({durum.bekleyenIstekSayisi}){/if}</a
            >
            <a href="/admin/users">Kullanıcılar</a>
            <a href="/admin/hareketler">Hareketler</a>
          {/if}
        </div>
        <div class="toolbar-right">
          {#if oturum.avatarUrl}
            <img src={oturum.avatarUrl} alt="avatar" class="toolbar-avatar" />
          {/if}
          <span>{oturum.currentUser}</span>
          <button class="cikis-btn" onclick={logout}>Çıkış</button>
        </div>
      </div>
    </div>

    {#if durum.bildirim}
      <div class="bildirim">{durum.bildirim}</div>
    {/if}

    <div class="sekme-icerik">
      {#if yolYetkiliMi($page.url.pathname, oturum.role)}
        {@render children()}
      {/if}
    </div>
  {/if}

  {#if durum.error}<p class="error">{durum.error}</p>{/if}
</main>
