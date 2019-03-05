import os
os.system("ng build --prod")
os.system("copy dist\\bmr\\index.html 404.html")
os.remove('dist/bmr/index.html')

s = open('404.html').read()
s = s.replace('"styles.', '"Zenith-MIDI/dist/bmr/styles.')
s = s.replace('"runtime', '"Zenith-MIDI/dist/bmr/runtime')
s = s.replace('"main', '"Zenith-MIDI/dist/bmr/main')
s = s.replace('"polyfills', '"Zenith-MIDI/dist/bmr/polyfills')
s = s.replace('assets/css-element-queries', 'Zenith-MIDI/dist/bmr/assets/css-element-queries')


def replaceAssetPaths(f, r):
    global s
    txt = open('dist/bmr/' + f).read()
    print('---', f)
    for root, _, files in os.walk('src/assets'):
        for _f in files:
            d = root.replace('\\', '/')[4:] + '/' + _f
            if d in txt:
                print('replaced', d, 'with', 'Zenith-MIDI/dist/bmr/' + d)
                txt = txt.replace(d, 'Zenith-MIDI/dist/bmr/' + d)
    s = s.replace(f, r)
    open('dist/bmr/' + r, 'w').write(txt)
    os.remove('dist/bmr/' + f)


dirs = os.listdir('dist/bmr')
print('Replacing asset paths')
for d in dirs:
    if d.startswith('main.'):
        replaceAssetPaths(d, 'main.ng.js')
    if d.startswith('styles.'):
        replaceAssetPaths(d, 'styles.ng.css')
    if d.startswith('runtime.'):
        replaceAssetPaths(d, 'runtime.ng.js')
    if d.startswith('polyfills.'):
        replaceAssetPaths(d, 'polyfills.ng.js')


open('404.html', 'w').write(s)
os.system("copy 404.html index.html")


os.system("git add .")
os.system("git commit -m \"deploy\"")
os.system("git push")
