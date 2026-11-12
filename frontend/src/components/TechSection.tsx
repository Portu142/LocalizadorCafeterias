import { Database, Layers3, Server } from 'lucide-react';

const technologies = [
  {
    icon: Server,
    title: 'API .NET 8',
    description:
      'ASP.NET Core Web API con arquitectura limpia, validación con FluentValidation y documentación Swagger.',
  },
  {
    icon: Database,
    title: 'PostgreSQL + PostGIS',
    description:
      'Búsqueda geoespacial por cercanía, índices GIST y migraciones gestionadas con EF Core.',
  },
  {
    icon: Layers3,
    title: 'React + TypeScript',
    description:
      'Interfaz responsive con React Query, Leaflet, Tailwind CSS y componentes accesibles.',
  },
];

export function TechSection() {
  return (
    <section id="proyecto" className="border-t border-coffee-100 bg-cream-50">
      <div className="mx-auto max-w-[1600px] px-4 py-16 lg:px-8">
        <div className="max-w-2xl">
          <span className="text-xs font-semibold uppercase tracking-[0.16em] text-caramel-600">
            Sobre el proyecto
          </span>
          <h2 className="mt-3 font-display text-3xl font-semibold tracking-tight text-coffee-950 sm:text-4xl">
            Un proyecto full-stack real
          </h2>
          <p className="mt-4 text-sm leading-6 text-coffee-600 sm:text-base">
            Este localizador está pensado como pieza de portafolio: backend .NET
            con PostgreSQL/PostGIS, frontend React y despliegue estático en GitHub
            Pages.
          </p>
        </div>

        <div className="mt-10 grid gap-4 md:grid-cols-3">
          {technologies.map((technology) => (
            <article key={technology.title} className="card p-6">
              <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-coffee-900 text-cream-50">
                <technology.icon className="h-5 w-5" />
              </span>
              <h3 className="mt-4 font-display text-xl font-semibold text-coffee-950">
                {technology.title}
              </h3>
              <p className="mt-2 text-sm leading-6 text-coffee-600">
                {technology.description}
              </p>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
