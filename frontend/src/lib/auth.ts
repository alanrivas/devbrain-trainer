const JWT_KEY = 'jwt';
const USER_KEY = 'user';

export interface User {
  id: string;
  email: string;
  username?: string;
}

/**
 * Guardar JWT en localStorage
 */
export const setToken = (token: string): void => {
  if (typeof window !== 'undefined') {
    localStorage.setItem(JWT_KEY, token);
  }
};

/**
 * Obtener JWT de localStorage
 */
export const getToken = (): string | null => {
  if (typeof window !== 'undefined') {
    return localStorage.getItem(JWT_KEY);
  }
  return null;
};

/**
 * Eliminar JWT de localStorage
 */
export const removeToken = (): void => {
  if (typeof window !== 'undefined') {
    localStorage.removeItem(JWT_KEY);
    localStorage.removeItem(USER_KEY);
  }
};

/**
 * Guardar datos de usuario en localStorage
 */
export const setUser = (user: User): void => {
  if (typeof window !== 'undefined') {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }
};

/**
 * Obtener datos de usuario de localStorage
 */
export const getUser = (): User | null => {
  if (typeof window !== 'undefined') {
    const user = localStorage.getItem(USER_KEY);
    return user ? JSON.parse(user) : null;
  }
  return null;
};

/**
 * Loguearse: guarda token y datos de usuario
 */
export const login = (token: string, user: User): void => {
  setToken(token);
  setUser(user);
};

/**
 * Desloguearse: elimina token y datos
 */
export const logout = (): void => {
  removeToken();
};

/**
 * Verificar si el usuario está autenticado
 */
export const isAuthenticated = (): boolean => {
  return getToken() !== null;
};
