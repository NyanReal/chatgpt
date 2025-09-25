(async () => {
  // ---- util: script loader (JSZip) ----
  const loadScript = (src) =>
    new Promise((res, rej) => {
      const s = document.createElement('script');
      s.src = src;
      s.onload = res;
      s.onerror = () => rej(new Error('failed:' + src));
      document.head.appendChild(s);
    });

  if (!window.JSZip) {
    await loadScript('https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js');
  }

  // ---- meta ----
  const title = (document.querySelector('meta[property="og:title"]')?.content || document.title || 'post').trim();
  const pageUrl = location.href.split('#')[0];
  const author =
    (document.querySelector('[rel="author"]')?.textContent ||
     document.querySelector('.article-header .user, .user-info .nick, .writer, .author')?.textContent ||
     document.querySelector('meta[name="author"]')?.content || '').trim();
  const datetime =
    (document.querySelector('time[datetime]')?.getAttribute('datetime') ||
     document.querySelector('.date, .time, .article-info time')?.textContent ||
     document.querySelector('time')?.textContent || '').trim();

  // ---- locate body ----
  const bodySelectors = [
    '#article-content', '.article-content',
    '.content .fr-view', '.fr-view',
    '.article-body', '.content-body',
    'article .content', '.markdown-body', '.article .content-body'
  ];
  let bodyEl = null;
  for (const sel of bodySelectors) {
    const el = document.querySelector(sel);
    if (el && el.offsetHeight > 0) { bodyEl = el; break; }
  }
  if (!bodyEl) { alert('본문을 찾지 못했어요.'); return; }

  // ---- clone & clean ----
  const clean = bodyEl.cloneNode(true);
  clean.querySelectorAll('script, style, iframe, video, audio, noscript').forEach(n => n.remove());
  clean.querySelectorAll('.btn, .buttons, .actions, .toolbar, .comment, .comments, .ad, [data-ad]').forEach(n => n.remove());

  // ---- normalize images (src/srcset/data-* 등) ----
  const imgs = Array.from(clean.querySelectorAll('img')).filter(img => (img.getAttribute('src') || img.getAttribute('data-src')));
  imgs.forEach(img => {
    if (!img.getAttribute('src') && img.getAttribute('data-src')) {
      img.setAttribute('src', img.getAttribute('data-src'));
    }
  });

  // ---- helpers ----
  const toAbs = (s) => { try { return new URL(s, document.baseURI).href; } catch { return s; } };
  const guessExt = (u) => {
    try {
      const p = new URL(u, document.baseURI).pathname;
      const ext = (p.split('.').pop() || '').split(/[?#]/)[0].toLowerCase();
      return /^(png|jpg|jpeg|gif|webp|avif|bmp|svg)$/.test(ext) ? ext : 'png';
    } catch { return 'png'; }
  };
  const esc = (s) => s.replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));

  // ---- fetch with namu.la proxy fallback ----
  const fetchArrayBuffer = async (src) => {
    try {
      const r = await fetch(src, { credentials: 'include' });
      if (!r.ok) throw new Error('HTTP ' + r.status);
      return await (await r.blob()).arrayBuffer();
    } catch (e) {
      try {
        const u = new URL(src, document.baseURI);
        if (u.hostname.endsWith('namu.la')) {
          // weserv.nl proxy (no credentials)
          const prox = 'https://images.weserv.nl/?url=' + encodeURIComponent(u.hostname + u.pathname + u.search);
          const r2 = await fetch(prox, { credentials: 'omit' });
          if (!r2.ok) throw new Error('HTTP ' + r2.status);
          return await (await r2.blob()).arrayBuffer();
        }
        throw e;
      } catch (e2) {
        throw e2;
      }
    }
  };

  // ---- zip build ----
  const zip = new JSZip();
  const imgDir = zip.folder('images');
  let savedCount = 0, idx = 1;

  for (const img of imgs) {
    const srcAbs = toAbs(img.getAttribute('src'));
    const ext = guessExt(srcAbs);
    const filename = `img_${String(idx).padStart(3, '0')}.${ext}`;
    try {
      const ab = await fetchArrayBuffer(srcAbs);
      img.setAttribute('src', `images/${filename}`);
      img.removeAttribute('srcset');
      img.removeAttribute('data-src');
      img.removeAttribute('loading');
      imgDir.file(filename, ab);
      savedCount++; idx++;
    } catch (err) {
      console.warn('이미지 저장 실패:', srcAbs, err);
      // 실패 시 원본 URL 유지
    }
  }

  // ---- html compose ----
  const headerHTML = `
    <header style="font:14px/1.5 system-ui,-apple-system,Segoe UI,Roboto,Arial,Apple SD Gothic Neo,Noto Sans KR,sans-serif;border-bottom:1px solid #ddd;padding:12px 0;margin-bottom:16px;">
      <div><strong>제목</strong>: ${esc(title)}</div>
      <div><strong>작성일</strong>: ${esc(datetime || 'Unknown')}</div>
      <div><strong>작성자</strong>: ${esc(author || 'Unknown')}</div>
      <div><strong>원문 링크</strong>: <a href="${esc(pageUrl)}">${esc(pageUrl)}</a></div>
    </header>
  `;
  const html = `<!doctype html>
<html lang="ko"><head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>${esc(title)}</title>
<style>
  body{max-width:960px;margin:0 auto;padding:24px;font:16px/1.7 system-ui,-apple-system,Segoe UI,Roboto,Arial,Apple SD Gothic Neo,Noto Sans KR,sans-serif;color:#111;background:#fff}
  img{max-width:100%;height:auto}
  pre,code{white-space:pre-wrap;word-break:break-word}
  table{border-collapse:collapse} td,th{border:1px solid #ddd;padding:6px}
</style>
</head><body>
${headerHTML}
<main>${clean.innerHTML}</main>
</body></html>`;

  zip.file('post.html', html);
  zip.file('meta.txt', [
    `Title: ${title}`,
    `Author: ${author || 'Unknown'}`,
    `Date: ${datetime || 'Unknown'}`,
    `Source: ${pageUrl}`,
    savedCount ? `Images: ${savedCount} files under /images` : `Images: (embedded URLs left if blocked)`
  ].join('\n'));

  const zipName = `arca-${(title || 'post').replace(/[\\/:*?"<>|]+/g,' ').trim().replace(/\s+/g,'-').slice(0,80) || 'post'}.zip`;
  const blob = await zip.generateAsync({ type: 'blob' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = zipName;
  document.body.appendChild(a);
  a.click();
  setTimeout(() => { URL.revokeObjectURL(a.href); a.remove(); }, 1000);
})();