import adapter from "@sveltejs/adapter-static";

/** @type {import("@sveltejs/kit").Config} */
export default {
  kit: {
    // SSR repo genelinde kapali (bkz. src/routes/+layout.js) - uygulama saf bir SPA,
    // bu yuzden statik adapter + SPA fallback kullaniliyor (sunucu calismiyor,
    // Docker build'i sonucu nginx tarafindan servis ediliyor).
    adapter: adapter({
      pages: "build",
      assets: "build",
      fallback: "index.html",
      precompress: false,
      strict: true,
    }),
    files: {
      assets: "public",
    },
  },
};
