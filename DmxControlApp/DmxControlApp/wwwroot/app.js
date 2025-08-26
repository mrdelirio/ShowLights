window.dmxAudio = (function () {
    let audioContext;
    let analyser;
    let rafId;
    let source;
    let dotnetRef;
    let canvas;
    let ctx;

    async function start(dotNetObjRef, canvasId) {
        dotnetRef = dotNetObjRef;
        canvas = document.getElementById(canvasId);
        if (canvas) {
            ctx = canvas.getContext('2d');
        }
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 2048;
        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);
        source = audioContext.createMediaStreamSource(stream);
        source.connect(analyser);

        function draw() {
            rafId = requestAnimationFrame(draw);
            analyser.getByteFrequencyData(dataArray);
            let sum = 0;
            for (let i = 0; i < dataArray.length; i++) sum += dataArray[i];
            const level = sum / (dataArray.length * 255);
            if (dotnetRef) {
                dotnetRef.invokeMethodAsync('OnAudioLevel', level);
            }
            if (ctx && canvas) {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                const barWidth = canvas.width / dataArray.length;
                for (let i = 0; i < dataArray.length; i++) {
                    const val = dataArray[i];
                    const h = (val / 255) * canvas.height;
                    ctx.fillStyle = `hsl(${(i / dataArray.length) * 360}, 80%, 50%)`;
                    ctx.fillRect(i * barWidth, canvas.height - h, barWidth, h);
                }
            }
        }
        draw();
    }

    async function stop() {
        if (rafId) cancelAnimationFrame(rafId);
        if (source) source.disconnect();
        if (audioContext) await audioContext.close();
        analyser = null; source = null; audioContext = null; dotnetRef = null; ctx = null; canvas = null;
    }

    return { start, stop };
})();

