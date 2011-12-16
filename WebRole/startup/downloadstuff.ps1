function download([string]$url) {
    $dest = $url.substring($url.lastindexof('/')+1)
    if (!(test-path $dest)) {
        (new-object system.net.webclient).downloadfile($url, $dest);
    }
}
foreach ($url in (
    'http://download.microsoft.com/download/3/2/2/3224B87F-CFA0-4E70-BDA3-3DE650EFEBA5/vcredist_x64.exe',
    'http://cloud.github.com/downloads/tjanczuk/iisnode/iisnode-full-iis7-v0.1.12-x64.msi',
    'http://nodejs.org/dist/v0.6.6/node-v0.6.6.msi',
    'http://msysgit.googlecode.com/files/PortableGit-1.7.6-preview20110709.7z'
)) { download $url }