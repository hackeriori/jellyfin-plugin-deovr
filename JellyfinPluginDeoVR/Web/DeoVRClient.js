// ==UserScript==
// @name JellyfinPluginDeoVR Client
// @version 3.1.2
// @description 在 Jellyfin 中浏览媒体并直接调用 DeoVR 播放 VR 视频的客户端脚本。
// @run-at document-end
// @author ChatGPT-4.1
// ==/UserScript==

(function() {
    'use strict';
    const SCRIPT_VERSION = '3.1.2';
    console.log(`[DeoVR v${SCRIPT_VERSION}] [Hardened+Fixed] Initializing...`);

    // --- 初始化设置 ---
    const pluginBasePath = '/deovr';
    const iconUrl = `${pluginBasePath}/Icon`;
    const buttonClassName = 'deovrButton';

    // --- 创建按钮 ---
    function buildDeeplinkUrl() {
        const hashParts = window.location.hash.split('?');
        const idFromUrl = (hashParts.length > 1)
            ? new URLSearchParams(hashParts[1]).get('id')
            : null;
        if (!idFromUrl) return null;
        const jsonUrl = `${window.location.origin}${pluginBasePath}/json/${idFromUrl}/response.json`;
        return `deovr://${jsonUrl}`;
    }
    function createDeoVRButton() {
        const deovrLink = document.createElement('a');
        deovrLink.className = 'button-flat detailButton ' + buttonClassName;
        deovrLink.setAttribute('is', 'emby-button');
        deovrLink.title = 'Open in DeoVR';
        deovrLink.style.marginLeft = '0.5em';
        deovrLink.style.display = 'inline-flex';
        deovrLink.style.alignItems = 'center';

        const buttonContent = document.createElement('div');
        buttonContent.className = 'detailButton-content';
        const iconImage = document.createElement('img');
        iconImage.src = iconUrl;
        iconImage.alt = 'DeoVR Icon';
        iconImage.style.width = '24px';
        buttonContent.appendChild(iconImage);
        deovrLink.appendChild(buttonContent);

        deovrLink.onclick = function(event) {
            event.preventDefault();
            const url = buildDeeplinkUrl();
            if (url) window.location.href = url;
            else alert('Unable to determine which movie to play in DeoVR');
        };
        return deovrLink;
    }

    // --- 按钮注入逻辑 ---
    function ensureDeoVRButtonPresent(container) {
        if (!container) return;
        if (!window.location.hash.includes('/details')) return;

        const playBtn = container.querySelector('.btnPlay');
        let deovrBtn = container.querySelector('.' + buttonClassName);

        // 若位置正确，则无需处理
        if (deovrBtn) {
            if (playBtn && playBtn.nextElementSibling === deovrBtn) return;
            if (!playBtn && container.lastElementChild === deovrBtn) return;
            // 位置不正确：先移除以便重新添加
            deovrBtn.remove();
        } else {
            deovrBtn = createDeoVRButton();
        }

        if (playBtn)
            playBtn.insertAdjacentElement('afterend', deovrBtn);
        else
            container.appendChild(deovrBtn);
    }

    function reinjectButtonsEverywhere() {
        document.querySelectorAll('.mainDetailButtons:not(.hide)').forEach(ensureDeoVRButtonPresent);
    }

    // --- DOM 变动监听器（MutationObserver）设置 ---
    const observer = new MutationObserver(mutations => {
        let shouldRefresh = false;
        for (const m of mutations) {
            if (
                (m.type === "attributes" && m.target.classList && m.target.classList.contains('mainDetailButtons')) ||
                (Array.from(m.addedNodes).some(node => node.nodeType === 1 && node.classList && node.classList.contains('mainDetailButtons'))) ||
                (Array.from(m.removedNodes).some(node => node.nodeType === 1 && node.classList && node.classList.contains('mainDetailButtons')))
            ) {
                shouldRefresh = true;
                break;
            }
        }
        if (shouldRefresh) reinjectButtonsEverywhere();
    });

    function safeStartup() {
        reinjectButtonsEverywhere();
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class']
        });
    }

    window.addEventListener('hashchange', () => setTimeout(reinjectButtonsEverywhere, 100));
    document.addEventListener('viewshow',   () => setTimeout(reinjectButtonsEverywhere, 100));
    document.addEventListener('pageshow',   () => setTimeout(reinjectButtonsEverywhere, 100));

    if (document.readyState === "complete" || document.readyState === "interactive")
        safeStartup();
    else
        window.addEventListener('DOMContentLoaded', safeStartup);

    // 后备兜底机制：定时周期性检查
    setInterval(reinjectButtonsEverywhere, 2000);

    console.log(`[DeoVR v${SCRIPT_VERSION}] Script active and resilient!`);
})();
