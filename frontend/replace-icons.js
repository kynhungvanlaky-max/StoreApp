const fs = require('fs');
const path = require('path');

const iconMap = {
    'search': 'fa-solid fa-magnifying-glass',
    'shopping_cart': 'fa-solid fa-cart-shopping',
    'shopping_c': 'fa-solid fa-cart-shopping',
    'person': 'fa-solid fa-user',
    'menu': 'fa-solid fa-bars',
    'home': 'fa-solid fa-house',
    'favorite': 'fa-solid fa-heart',
    'delete': 'fa-solid fa-trash',
    'edit': 'fa-solid fa-pen',
    'add': 'fa-solid fa-plus',
    'remove': 'fa-solid fa-minus',
    'check': 'fa-solid fa-check',
    'close': 'fa-solid fa-xmark',
    'arrow_forward': 'fa-solid fa-arrow-right',
    'arrow_back': 'fa-solid fa-arrow-left',
    'arrow_forward_ios': 'fa-solid fa-chevron-right',
    'arrow_back_ios_new': 'fa-solid fa-chevron-left',
    'expand_more': 'fa-solid fa-chevron-down',
    'expand_less': 'fa-solid fa-chevron-up',
    'local_fire_department': 'fa-solid fa-fire',
    'smart_toy': 'fa-solid fa-robot',
    'storefront': 'fa-solid fa-store',
    'library_books': 'fa-solid fa-book',
    'article': 'fa-solid fa-newspaper',
    'feed': 'fa-solid fa-rss',
    'sports_esports': 'fa-solid fa-gamepad',
    'payments': 'fa-solid fa-credit-card',
    'key': 'fa-solid fa-key',
    'content_copy': 'fa-solid fa-copy',
    'info': 'fa-solid fa-circle-info',
    'calendar_today': 'fa-solid fa-calendar-days',
    'share': 'fa-solid fa-share-nodes',
    'link': 'fa-solid fa-link',
    'thumb_up': 'fa-solid fa-thumbs-up',
    'chat_bubble': 'fa-solid fa-comment',
    'email': 'fa-solid fa-envelope',
    'lock': 'fa-solid fa-lock',
    'visibility': 'fa-solid fa-eye',
    'visibility_off': 'fa-solid fa-eye-slash',
    'login': 'fa-solid fa-right-to-bracket',
    'logout': 'fa-solid fa-right-from-bracket',
    'error': 'fa-solid fa-circle-exclamation',
    'warning': 'fa-solid fa-triangle-exclamation',
    'check_circle': 'fa-solid fa-circle-check',
    'inventory_2': 'fa-solid fa-box',
    'filter_alt': 'fa-solid fa-filter',
    'category': 'fa-solid fa-layer-group',
    'sell': 'fa-solid fa-tag',
    'memory': 'fa-solid fa-microchip',
    'sd_storage': 'fa-solid fa-hard-drive',
    'video_camera_front': 'fa-solid fa-video',
    'language': 'fa-solid fa-globe',
    'receipt_long': 'fa-solid fa-receipt',
    'search_off': 'fa-solid fa-magnifying-glass-minus',
    'add_shopping_cart': 'fa-solid fa-cart-plus',
    'swords': 'fa-solid fa-khanda',
    'explore': 'fa-solid fa-compass',
    'chess': 'fa-solid fa-chess-knight',
    'sports_soccer': 'fa-solid fa-futbol',
    'more_vert': 'fa-solid fa-ellipsis-vertical',
    'send': 'fa-solid fa-paper-plane',
    'attach_file': 'fa-solid fa-paperclip',
    'mic': 'fa-solid fa-microphone',
    'settings': 'fa-solid fa-gear',
    'help': 'fa-solid fa-circle-question',
    'play_arrow': 'fa-solid fa-play',
    'pause': 'fa-solid fa-pause',
    'stop': 'fa-solid fa-stop',
    'volume_up': 'fa-solid fa-volume-high',
    'volume_off': 'fa-solid fa-volume-xmark',
    'fullscreen': 'fa-solid fa-expand',
    'download': 'fa-solid fa-download',
    'upload': 'fa-solid fa-upload',
    'sync': 'fa-solid fa-rotate',
    'refresh': 'fa-solid fa-arrow-rotate-right',
    'star': 'fa-solid fa-star',
    'star_border': 'fa-regular fa-star',
    'star_half': 'fa-solid fa-star-half-stroke',
    'done': 'fa-solid fa-check',
    'done_all': 'fa-solid fa-check-double',
    'account_circle': 'fa-solid fa-circle-user',
    'history': 'fa-solid fa-clock-rotate-left',
    'receipt': 'fa-solid fa-file-invoice'
};

function walkDir(dir, callback) {
    fs.readdirSync(dir).forEach(f => {
        let dirPath = path.join(dir, f);
        let isDirectory = fs.statSync(dirPath).isDirectory();
        isDirectory ? walkDir(dirPath, callback) : callback(path.join(dir, f));
    });
}

const dir = path.join(__dirname, 'Components');

walkDir(dir, function(filePath) {
    if (filePath.endsWith('.razor')) {
        let content = fs.readFileSync(filePath, 'utf8');
        let modified = false;
        
        const regex = /<span class="material-symbols-outlined([^"]*)">([\s\S]*?)<\/span>/g;
        
        let newContent = content.replace(regex, (match, classes, iconName) => {
            iconName = iconName.trim();
            const mappedClass = iconMap[iconName] || `fa-solid fa-${iconName.replace(/_/g, '-')}`;
            modified = true;
            return `<i class="${mappedClass}${classes}"></i>`;
        });
        
        if (modified) {
            fs.writeFileSync(filePath, newContent, 'utf8');
            console.log(`Updated ${filePath}`);
        }
    }
});
