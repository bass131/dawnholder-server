const assets = import.meta.glob<string>('./assets/*.png', { query: '?url', import: 'default', eager: true });
export default function ThemeImage({ name, className = '' }: { name: string; className?: string }) {
  const src = assets[`./assets/${name}.png`];
  return src ? <img src={src} alt="" aria-hidden="true" className={`pixel-image ${className}`} draggable={false} /> : <span aria-hidden="true" className={`image-fallback ${className}`}>D</span>;
}
