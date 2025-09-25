(async () => {
  const loadScript = (src) =>
    new Promise((res, rej) => {
      const s = document.createElement('script');
      s.src = src;
      s.onload = () => res();
      s.onerror = () => rej(new Error('failed:' + src));
      document.head.appendChild(s);
    });

  // JSZip 로드 (UMD)
  if (!window.JSZip) {
    await loadScript('https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js');
  }

  // 기본 메타
  const title = (document.querySelector('meta[property="og:title"]')?.content || document.title || 'post').trim();
  const url = location.href.split('#')[0];
  const author =
    (document.querySelector('[rel="author"]')?.textContent ||
     document.querySelector('.article-header .user, .user-info .nick, .writer, .author')?.textContent ||
     document.querySelector('meta[name="author"]')?.content || '').trim();
  const datetime =
    (document.querySelector('time[datetime]')?.getAttribute('datetime') ||
     document.querySelector('time')?.textContent ||
     document.querySelector('.date, .time, .article-info time')?.textContent || '').trim();

  // 본문 후보 셀렉터들(아카/포럼류 범용)
  const bodySelectors = [
    '#article-content', '.article-content', '.content .fr-view', '.fr-view', '.article-body',
    '.content-body', 'article .content', '.markdown-body', '.article .content-body'
  ];
  let bodyEl = null;
  for (const sel of bodySelectors) {
    const el = document.querySelector(sel);
    if (el && el.offsetHeight > 0) { bodyEl = el; break; }
  }
  if (!bodyEl) {
    alert('본문을 찾지 못했어요. 페이지 구조가 달라졌을 수 있어요.');
    return;
  }

  // 클론해서 불필요 요소 제거
  const clean = bodyEl.cloneNode(true);
  clean.querySelectorAll('script, style, iframe, video, audio, noscript').forEach(n => n.remove());
  // 접기/댓글/버튼류 흔한 것들 제거
  clean.querySelectorAll('.btn, .buttons, .actions, .toolbar, .comment, .comments, .ad, [data-ad]').forEach(n => n.remove());

  // 이미지 수집 & 경로 치환
  const imgs = Array.from(clean.querySelectorAll('img')).filter(img => (img.getAttribute('src') || img.getAttribute('data-src')));
  const zip = new JSZip();
  const imgDir = zip.folder('images');
  const downloaded = [];
  let idx = 1;

  // src 정규화
  const getAbs = (s) => {
    try { return new URL(s, document.baseURI).href; } catch { return s; }
  };

  // data-src → src 승격
  imgs.forEach(img => {
    if (!img.getAttribute('src') && img.getAttribute('data-src')) {
      img.setAttribute('src', img.getAttribute('data-src'));
    }
  });

  for (const img of imgs) {
    const src = getAbs(img.getAttribute('src'));
    const urlObj = new URL(src, document.baseURI);
    const extGuess = (urlObj.pathname.split('.').pop() || 'png').split(/[?#]/)[0].toLowerCase();
    const safeExt = extGuess.match(/^(png|jpg|jpeg|gif|webp|avif|bmp|svg)$/) ? extGuess : 'png';
    const filename = `img_${String(idx).padStart(3, '0')}.${safeExt}`;

    try {
      const resp = await fetch(src, { credentials: 'include' });
      if (!resp.ok) throw new Error('HTTP ' + resp.status);
      const blob = await resp.blob();
      const ab = await blob.arrayBuffer();
      img.setAttribute('src', `images/${filename}`); // 본문 내 경로 교체
      img.removeAttribute('srcset');
      img.removeAttribute('data-src');
      img.removeAttribute('loading');
      imgDir.file(filename, ab);
      downloaded.push(filename);
      idx++;
    } catch (e) {
      // CORS 등으로 실패 시 원본 URL 유지 + 리스트에 표기
      console.warn('이미지 다운로드 실패:', src, e);
    }
  }

  // HTML 문서 구성
  const esc = (s) => s.replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
  const headerHTML = `
    <header style="font:14px/1.5 system-ui, -apple-system, Segoe UI, Roboto, Arial, Apple SD Gothic Neo, Noto Sans KR, sans-serif; border-bottom:1px solid #ddd; padding:12px 0; margin-bottom:16px;">
      <div><strong>제목</strong>: ${esc(title)}</div>
      <div><strong>작성일</strong>: ${esc(datetime || 'Unknown')}</div>
      <div><strong>작성자</strong>: ${esc(author || 'Unknown')}</div>
      <div><strong>원문 링크</strong>: <a href="${esc(url)}">${esc(url)}</a></div>
    </header>
  `;

  const docHTML = `<!doctype html>
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

  zip.file('post.html', docHTML);

  // 메타 요약 텍스트도 같이(옵션)
  const metaTxt = [
    `Title: ${title}`,
    `Author: ${author || 'Unknown'}`,
    `Date: ${datetime || 'Unknown'}`,
    `Source: ${url}`,
    downloaded.length ? `Images: ${downloaded.length} files under /images` : `Images: (embedded URLs left if CORS blocked)`
  ].join('\n');
  zip.file('meta.txt', metaTxt);

  // ZIP 파일명
  const slug = (title || 'post').replace(/[\\/:*?"<>|]+/g,' ').trim().replace(/\s+/g,'-').slice(0,80);
  const zipName = `arca-${slug || 'post'}.zip`;

  const blob = await zip.generateAsync({ type: 'blob' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = zipName;
  document.body.appendChild(a);
  a.click();
  setTimeout(() => {
    URL.revokeObjectURL(a.href);
    a.remove();
  }, 1000);
})();