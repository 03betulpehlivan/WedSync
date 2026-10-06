// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Guest Session Management
function getGuestSessionId() {
    let sessionId = localStorage.getItem('guestSessionId');
    if (!sessionId) {
        sessionId = 'g_' + Math.random().toString(36).substr(2, 9) + '_' + Date.now();
        localStorage.setItem('guestSessionId', sessionId);
    }
    return sessionId;
}
